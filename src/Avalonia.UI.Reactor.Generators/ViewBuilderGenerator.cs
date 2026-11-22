// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avalonia.UI.Reactor.Generators;

/// <summary>
/// Lowers the view builder blocks: each view statement of a block (<c>Text("a");</c>) is intercepted by a call that
/// creates the view as written, then adds it to the block being collected.
/// </summary>
/// <remarks>
/// <para>
/// There is one interceptor per intercepted method, carrying one <c>[InterceptsLocation]</c> per call, with a name
/// derived from the method: adding, moving or removing a call never renames an interceptor, which hot reload refuses.
/// A private method of the application is called through <c>[UnsafeAccessor]</c>, without reflection.
/// </para>
/// <para>
/// The calls are resolved in the compilation plus the code of the <c>[State]</c> properties: generators do not see each
/// other's code, and a block that uses a binding (<c>TextField(Bind.Email)</c>) would not resolve without it.
/// </para>
/// <para>
/// The generator also registers the assembly, so that a block compiled without it is reported instead of losing its
/// views (<c>ViewCollector.RegisterAssembly</c>).
/// </para>
/// </remarks>
[Generator]
public sealed class ViewBuilderGenerator : IIncrementalGenerator
{
    // The namespace of the generated code, which the projects allow for interceptors (Avalonia.UI.Reactor.Core.targets).
    private const string GeneratedNamespace = "Avalonia.UI.Reactor.Generated";

    private const string Collector = "global::Avalonia.UI.Reactor.Runtime.ViewCollector";
    private const string CollectorMetadataName = "Avalonia.UI.Reactor.Runtime.ViewCollector";

    // Placeholders of the text of an interceptor, filled when the calls of a method are grouped.
    private const string NamePlaceholder = "{{NAME}}";
    private const string AttributesPlaceholder = "{{ATTRIBUTES}}";

    private static readonly SymbolDisplayFormat s_typeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<SyntaxNode> statements = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax } or InvocationExpressionSyntax
                && ViewBuilderSyntax.BlockLambdaOf(node) is not null,
            static (syntax, _) => syntax.Node);

        IncrementalValueProvider<ImmutableArray<Site>> sites = statements.Collect()
            .Combine(WithStateCode(context))
            .SelectMany(static (input, cancellation) => Describe(input.Left, input.Right, cancellation))
            .Collect();

        IncrementalValueProvider<string?> registration = context.CompilationProvider.Select(static (compilation, _) =>
            compilation.GetTypeByMetadataName(CollectorMetadataName) is null ? null : compilation.AssemblyName);

        context.RegisterSourceOutput(sites, static (output, all) => EmitInterceptors(output, all));
        context.RegisterSourceOutput(registration, static (output, assembly) => EmitRegistration(output, assembly));
    }

    // The compilation in which the calls of the blocks are resolved: the code of the application and the code of the
    // [State] properties. A generator does not see what the others write, so without it a block that uses a binding
    // (TextField(Bind.Email)) would not resolve, and none of its views would be collected.
    private static IncrementalValueProvider<Compilation> WithStateCode(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<SyntaxTree>> stateCode = StateGenerator.States(context).Collect()
            .Combine(context.ParseOptionsProvider)
            .Select(static (input, cancellation) => StateGenerator.Sources(input.Left)
                .Select(source => CSharpSyntaxTree.ParseText(source.Text, (CSharpParseOptions)input.Right, source.HintName, cancellationToken: cancellation))
                .ToImmutableArray());

        return context.CompilationProvider.Combine(stateCode)
            .Select(static (input, _) => input.Left.AddSyntaxTrees(input.Right));
    }

    // One semantic model per file, shared by its statements: the body that contains them is bound once.
    private static IEnumerable<Site> Describe(ImmutableArray<SyntaxNode> statements, Compilation compilation, CancellationToken cancellation)
    {
        foreach (IGrouping<SyntaxTree, SyntaxNode> file in statements.GroupBy(s => s.SyntaxTree))
        {
            SemanticModel model = compilation.GetSemanticModel(file.Key);
            foreach (Site site in file.SelectMany(statement => Describe(statement, model, cancellation)))
                yield return site;
        }
    }

    // The calls of a statement to intercept: the call of the statement, which adds its view to the block, and, when the
    // statement starts with a view method (Progress(); or Progress().Padding(8);), the call of that method, which becomes
    // a scope run later.
    private static IEnumerable<Site> Describe(SyntaxNode statement, SemanticModel model, CancellationToken cancellation)
    {
        InvocationExpressionSyntax invocation = statement as InvocationExpressionSyntax
            ?? (InvocationExpressionSyntax)((ExpressionStatementSyntax)statement).Expression;
        if (!ViewBuilderSyntax.IsBlockStatement(statement, model, cancellation)
            || model.GetSymbolInfo(invocation, cancellation).Symbol is not IMethodSymbol method
            || !ViewBuilderSyntax.IsInterceptable(method))
        {
            yield break;
        }

        InvocationExpressionSyntax head = HeadOf(invocation);
        bool scoped = model.GetSymbolInfo(head, cancellation).Symbol is IMethodSymbol headMethod
            && ViewBuilderSyntax.IsScopedViewMethod(headMethod);
        if (scoped && head != invocation && SiteOf(head, model, InterceptionKind.Scope, cancellation) is { } scope)
            yield return scope;

        if (SiteOf(invocation, model, scoped && head == invocation ? InterceptionKind.EmitScope : InterceptionKind.Emit, cancellation) is { } site)
            yield return site;
    }

    // The first call of a chain of modifiers: Progress() in Progress().Padding(8).
    private static InvocationExpressionSyntax HeadOf(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax receiver }
            ? HeadOf(receiver)
            : invocation;

    private static Site? SiteOf(InvocationExpressionSyntax invocation, SemanticModel model, InterceptionKind kind, CancellationToken cancellation)
    {
        var method = (IMethodSymbol)model.GetSymbolInfo(invocation, cancellation).Symbol!;
#pragma warning disable RSEXPERIMENTAL002 // The interceptable location of a call is an experimental API of Roslyn.
        InterceptableLocation? location = model.GetInterceptableLocation(invocation, cancellation);
#pragma warning restore RSEXPERIMENTAL002
        if (location is null)
            return null;

        IMethodSymbol definition = (method.ReducedFrom ?? method).OriginalDefinition;
        return new Site(
            KeyOf(definition, kind),
            InterceptorOf(definition, kind),
            location.GetInterceptsLocationAttributeSyntax(),
            invocation.SyntaxTree.FilePath,
            invocation.SpanStart);
    }

    // A stable name per intercepted method and kind of interception, made of its type, its name and the types of its
    // parameters.
    private static string KeyOf(IMethodSymbol method, InterceptionKind kind)
    {
        string signature = method.ContainingType.ToDisplayString() + "_" + method.Name + "_"
            + string.Join("_", method.Parameters.Select(p => p.Type.ToDisplayString()));
        string prefix = kind switch
        {
            InterceptionKind.EmitScope => "ScopedView_",
            InterceptionKind.Scope => "Scope_",
            _ => "View_",
        };
        return prefix + new string([.. signature.Select(c => char.IsLetterOrDigit(c) ? c : '_')]);
    }

    // The text of the interceptor of a method: it calls the method as written, then emits its view; or, for a view
    // method, it returns a scope that calls the method later, with the receiver and the arguments as the inputs that the
    // reconciler compares. A method of the application that the generated code cannot reach is called through an
    // UnsafeAccessor.
    private static string InterceptorOf(IMethodSymbol method, InterceptionKind kind)
    {
        string returnType = method.ReturnType.ToDisplayString(s_typeFormat);
        string container = method.ContainingType.ToDisplayString(s_typeFormat);
        string typeParameters = method.IsGenericMethod ? "<" + string.Join(", ", method.TypeParameters.Select(t => t.Name)) + ">" : "";
        string constraints = string.Concat(method.TypeParameters.Select(ConstraintsOf));
        string arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
        List<string> parameters = [.. method.Parameters.Select((p, i) =>
            (i == 0 && method.IsExtensionMethod ? "this " : "") + (p.IsParams ? "params " : "") + p.Type.ToDisplayString(s_typeFormat) + " " + p.Name)];

        var text = new StringBuilder();
        string call;
        if (ViewBuilderSyntax.IsAccessibleFromAssembly(method))
        {
            if (method.IsStatic)
            {
                call = $"{container}.{method.Name}{typeParameters}({arguments})";
            }
            else
            {
                parameters.Insert(0, $"this {container} __self");
                call = $"__self.{method.Name}{typeParameters}({arguments})";
            }
        }
        else
        {
            string target = method.IsStatic ? $"{container}? __type" : $"{container} __self";
            string accessorKind = method.IsStatic ? "StaticMethod" : "Method";
            string accessorParameters = string.Join(", ", new[] { target }.Concat(method.Parameters.Select(p => p.Type.ToDisplayString(s_typeFormat) + " " + p.Name)));
            text.AppendLine($"        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.{accessorKind}, Name = \"{method.Name}\")]");
            text.AppendLine($"        private static extern {returnType} Call{NamePlaceholder}({accessorParameters});");
            text.AppendLine();
            if (!method.IsStatic)
                parameters.Insert(0, $"this {container} __self");
            string self = method.IsStatic ? "null" : "__self";
            call = $"Call{NamePlaceholder}({self}{(arguments.Length > 0 ? ", " + arguments : "")})";
        }

        string body = kind == InterceptionKind.Emit ? $"{Collector}.Emit({call})" : ScopeOf(method, call);
        if (kind == InterceptionKind.EmitScope)
            body = $"{Collector}.Emit({body})";

        text.AppendLine($"        {AttributesPlaceholder}");
        text.AppendLine($"        public static {returnType} {NamePlaceholder}{typeParameters}({string.Join(", ", parameters)}){constraints}");
        text.AppendLine($"            => {body};");
        return text.ToString();
    }

    // The scope of a view method: its name identifies it, and its receiver and arguments are its inputs.
    private static string ScopeOf(IMethodSymbol method, string call)
    {
        List<string> inputs = [.. method.Parameters.Select(p => p.Name)];
        if (!method.IsStatic)
            inputs.Insert(0, "__self");

        string tuple = inputs.Count == 0
            ? "default(global::System.ValueTuple)"
            : $"global::System.ValueTuple.Create({string.Join(", ", inputs)})";
        string name = SymbolDisplay.FormatLiteral(method.ToDisplayString(), quote: true);
        return $"{Collector}.ViewMethod({name}, {tuple}, () => {call})";
    }

    private static string ConstraintsOf(ITypeParameterSymbol parameter)
    {
        List<string> constraints = [];
        if (parameter.HasReferenceTypeConstraint)
            constraints.Add("class");
        if (parameter.HasValueTypeConstraint)
            constraints.Add("struct");
        if (parameter.HasUnmanagedTypeConstraint)
            constraints.Add("unmanaged");
        if (parameter.HasNotNullConstraint)
            constraints.Add("notnull");
        constraints.AddRange(parameter.ConstraintTypes.Select(t => t.ToDisplayString(s_typeFormat)));
        if (parameter.HasConstructorConstraint)
            constraints.Add("new()");
        return constraints.Count == 0 ? "" : $" where {parameter.Name} : {string.Join(", ", constraints)}";
    }

    private static void EmitInterceptors(SourceProductionContext output, ImmutableArray<Site> sites)
    {
        if (sites.IsEmpty)
            return;

        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("#nullable enable");
        text.AppendLine();
        text.AppendLine("namespace System.Runtime.CompilerServices");
        text.AppendLine("{");
        text.AppendLine("    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]");
        text.AppendLine("    file sealed class InterceptsLocationAttribute : global::System.Attribute");
        text.AppendLine("    {");
        text.AppendLine("        public InterceptsLocationAttribute(int version, string data)");
        text.AppendLine("        {");
        text.AppendLine("        }");
        text.AppendLine("    }");
        text.AppendLine("}");
        text.AppendLine();
        text.AppendLine($"namespace {GeneratedNamespace}");
        text.AppendLine("{");
        text.AppendLine("    [global::System.Diagnostics.DebuggerStepThrough]");
        text.AppendLine("    file static class ViewBuilderInterceptors");
        text.AppendLine("    {");
        bool first = true;
        foreach (IGrouping<string, Site> method in sites.GroupBy(s => s.Key).OrderBy(g => g.Key, System.StringComparer.Ordinal))
        {
            if (!first)
                text.AppendLine();
            first = false;
            string attributes = string.Join("\n        ", method
                .OrderBy(s => s.File, System.StringComparer.Ordinal)
                .ThenBy(s => s.Position)
                .Select(s => s.Attribute));
            text.Append(method.First().Interceptor.Replace(NamePlaceholder, method.Key).Replace(AttributesPlaceholder, attributes));
        }

        text.AppendLine("    }");
        text.AppendLine("}");
        output.AddSource("ViewBuilderInterceptors.g.cs", text.ToString());
    }

    // Registers the assembly at load, so that a block of an assembly compiled without the generator is reported. The
    // name of the class is unique per assembly: an assembly that sees the internals of another one sees its class too.
    private static void EmitRegistration(SourceProductionContext output, string? assembly)
    {
        if (assembly is null)
            return;

        string name = "ViewBuilderModule_" + new string([.. assembly.Select(c => char.IsLetterOrDigit(c) ? c : '_')]);
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine();
        text.AppendLine($"namespace {GeneratedNamespace}");
        text.AppendLine("{");
        text.AppendLine($"    internal static class {name}");
        text.AppendLine("    {");
        text.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
        text.AppendLine($"        internal static void Register() => {Collector}.RegisterAssembly(typeof({name}).Assembly);");
        text.AppendLine("    }");
        text.AppendLine("}");
        output.AddSource("ViewBuilderModule.g.cs", text.ToString());
    }

    // One intercepted call: the interceptor of its method, its location, and its place for a stable order.
    private sealed record Site(string Key, string Interceptor, string Attribute, string File, int Position);
}
