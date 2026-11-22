// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.UI.Reactor.Generators;

/// <summary>
/// Reports the pitfalls of reactive state and views at compile time.
/// </summary>
/// <remarks>
/// The analyzer reports a badly declared <c>[State]</c> property (MVU001–004), a member displayed by a view but changed
/// without being observed, so that the interface would not update (MVU010–011), state passed to a control without its
/// binding, so that the control would not change it (MVU012), a view created but never displayed (MVU013), a component
/// with state created in a view, which would lose its state on every update (MVU016), and a task that ignores its
/// cancellation token, which would keep running after its view disappears (MVU017).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StateAnalyzer : DiagnosticAnalyzer
{
    // Messages in the format of the Microsoft analyzers: a short title without a final period, a one-sentence message
    // without a final period with names in single quotes, and a description in complete sentences (why, and how to fix).
    private const string Category = "Usage";

    /// <summary>
    /// Describes MVU001: a <c>[State]</c> property that is not <see langword="partial"/>.
    /// </summary>
    public static readonly DiagnosticDescriptor NotPartialProperty = new(
        "MVU001",
        title: "State property must be partial",
        messageFormat: "State property '{0}' must be declared 'partial', for example '[State] partial {1} {0} {{ get; set; }}'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "The implementation of a [State] property is generated. Declare the property as 'partial', with accessors that have no body.");

    /// <summary>
    /// Describes MVU002: a type that declares <c>[State]</c> properties but is not <see langword="partial"/>.
    /// </summary>
    public static readonly DiagnosticDescriptor NotPartialType = new(
        "MVU002",
        title: "Type declaring state properties must be partial",
        messageFormat: "Type '{0}' must be declared 'partial' because it contains [State] properties",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "The generator adds members to the type that declares [State] properties and to each of its containing types, so all of them must be partial.");

    /// <summary>
    /// Describes MVU003: a <c>[State]</c> property with a body, a static one, or one without a getter.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidShape = new(
        "MVU003",
        title: "Invalid state property declaration",
        messageFormat: "State property '{0}' {1}",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "A [State] property must be an instance property with a 'get' accessor, and its accessors must not have a body because its implementation is generated.");

    /// <summary>
    /// Describes MVU004: <c>Bind</c> or <c>StateBindings</c> declared by hand in a class that has settable state.
    /// </summary>
    public static readonly DiagnosticDescriptor ReservedName = new(
        "MVU004",
        title: "Name reserved for generated bindings",
        messageFormat: "'{0}' is reserved for the bindings generated for the [State] properties of '{1}'; rename this member",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "The generator adds a 'Bind' property and a 'StateBindings' type to every class that declares settable [State] properties. A member with one of these names prevents the bindings from being generated.");

    // The names that the generator writes in every class that has settable state.
    private static readonly ImmutableHashSet<string> s_reservedNames = ImmutableHashSet.Create("Bind", "StateBindings");

    /// <summary>
    /// Describes MVU010: a member displayed by a view and changed, but not observed.
    /// </summary>
    public static readonly DiagnosticDescriptor UnobservedMember = new(
        "MVU010",
        title: "Displayed member is not observable",
        messageFormat: "'{0}' is read by the view but is not observable, so changing it does not update the UI; declare it as '[State] partial {1} {2} {{ get; set; }}' or call 'Refresh()'",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        description: "A field or auto-property that a view reads (in 'Body' or in a method that returns a 'View') and that is modified elsewhere does not notify the view. Make it a [State] property, or call 'Refresh()' after changing it.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>
    /// Describes MVU011: a <see cref="System.Collections.Generic.List{T}"/> displayed by a view and changed.
    /// </summary>
    public static readonly DiagnosticDescriptor UnobservedList = new(
        "MVU011",
        title: "Displayed list is not observable",
        messageFormat: "List '{0}' is read by the view but is not observable, so changing it does not update the UI; use 'StateList<{1}>'",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        description: "Adding, removing or replacing the items of a 'List<T>' does not notify the view. Use 'StateList<T>', which notifies the views that enumerate it.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>
    /// Describes MVU012: state passed to a control without its binding.
    /// </summary>
    public static readonly DiagnosticDescriptor UnboundState = new(
        "MVU012",
        title: "State property passed without its binding",
        messageFormat: "State property '{0}' is passed by value, so '{1}' cannot update it; pass '{2}' instead",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "A control that supports two-way binding only displays a [State] property that is passed by value. Pass its generated binding ('Bind.Name', or 'item.Bind.Name' for a model object) so that the control can update it. To make the control read-only on purpose, pass 'null' as the change handler.");

    /// <summary>
    /// Describes MVU013: a view created as a statement, whose value is not used.
    /// </summary>
    public static readonly DiagnosticDescriptor DiscardedView = new(
        "MVU013",
        title: "View is created but never displayed",
        messageFormat: "The view returned by '{0}' is not used, so it is never displayed; pass it to a container such as 'VStack', or return it",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "A view is a description that appears only when 'Body' returns it or a container receives it. A view created as a statement, whose value is not used, is never displayed. Pass it to a container ('VStack(a, b)'), return it, or remove the statement.");

    /// <summary>
    /// Describes MVU016: a component with state created in a view.
    /// </summary>
    public static readonly DiagnosticDescriptor ComponentCreatedInView = new(
        "MVU016",
        title: "Component is created in a view",
        messageFormat: "'{0}' is created on every update of the view, so its state is lost each time; keep the instance in a field, or move its state into the item it displays",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "A component keeps its state as long as the same instance is displayed. Created in 'Body' or in a method that returns a view, it is a new instance on every update, whose [State] properties start again from their initial values. Create the component once, in a field or a constructor, or move its state into the model it displays and draw it with a method.");

    /// <summary>
    /// Describes MVU017: a task started by <c>.Task(…)</c> that ignores its cancellation token.
    /// </summary>
    public static readonly DiagnosticDescriptor IgnoredCancellation = new(
        "MVU017",
        title: "Task ignores its cancellation token",
        messageFormat: "The task passed to 'Task' ignores its cancellation token, so it keeps running after its view disappears; pass the token to the calls it awaits",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        description: "A task started by '.Task(…)' is stopped through the token it receives, which is cancelled when its view disappears, its key changes or the window closes. A task that never uses the token goes on: a timer or a request keeps running after the view is gone. Pass the token to the asynchronous calls that the task awaits, or check it in its loop.");

    // The name of the modifier that runs a task while its view is displayed.
    private const string TaskModifierName = "Task";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            NotPartialProperty, NotPartialType, InvalidShape, ReservedName, UnobservedMember, UnobservedList, UnboundState,
            DiscardedView, ComponentCreatedInView, IgnoredCancellation);

    private static readonly ImmutableHashSet<string> s_listMutators = ImmutableHashSet.Create(
        "Add", "AddRange", "Insert", "InsertRange", "Remove", "RemoveAt", "RemoveAll", "RemoveRange", "Clear", "Sort", "Reverse");

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? stateAttr = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.State.StateAttribute");
            INamedTypeSymbol? component = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.State.Component");
            INamedTypeSymbol? view = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.Views.View");
            INamedTypeSymbol? list = start.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            if (stateAttr is null || component is null || view is null)
                return;

            start.RegisterSyntaxNodeAction(c => CheckStateDeclaration(c, stateAttr), SyntaxKind.PropertyDeclaration);

            // Members written by hand under a name reserved for the generated bindings (generated code is not analyzed).
            start.RegisterSyntaxNodeAction(c => CheckReservedName(c, stateAttr),
                SyntaxKind.PropertyDeclaration, SyntaxKind.MethodDeclaration, SyntaxKind.FieldDeclaration,
                SyntaxKind.EventDeclaration, SyntaxKind.EventFieldDeclaration, SyntaxKind.DelegateDeclaration,
                SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration,
                SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration, SyntaxKind.EnumDeclaration);

            INamedTypeSymbol? binding = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.State.Binding`1");
            if (binding is not null)
                start.RegisterSyntaxNodeAction(c => CheckUnboundState(c, stateAttr, binding), SyntaxKind.InvocationExpression);

            start.RegisterOperationAction(c => CheckDiscardedView(c, view), OperationKind.ExpressionStatement);
            start.RegisterOperationAction(c => CheckComponentCreatedInView(c, component, view, stateAttr), OperationKind.ObjectCreation);

            INamedTypeSymbol? modifiers = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.Views.Modifiers");
            INamedTypeSymbol? componentModifiers = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.Views.ComponentModifiers");
            INamedTypeSymbol? token = start.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
            if (modifiers is not null && componentModifiers is not null && token is not null)
            {
                start.RegisterOperationAction(
                    c => CheckIgnoredCancellation(c, modifiers, componentModifiers, token), OperationKind.Invocation);
            }

            // Members read by a view and changed elsewhere: matched at the end of the compilation.
            var readInView = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
            var writes = new ConcurrentBag<(ISymbol Member, Location Location, bool IsList)>();

            start.RegisterOperationAction(c =>
            {
                ISymbol? member = c.Operation switch
                {
                    IFieldReferenceOperation f => f.Field,
                    IPropertyReferenceOperation p => p.Property,
                    _ => null,
                };
                if (member is null || member.IsStatic || !IsComponentMember(member, component))
                    return;
                if (IsWriteTarget(c.Operation) || InsideLambda(c.Operation))
                    return;
                if (IsViewMember(c.ContainingSymbol, view))
                    readInView.TryAdd(member, 0);
            }, OperationKind.FieldReference, OperationKind.PropertyReference);

            start.RegisterOperationAction(c =>
            {
                IOperation? target = c.Operation switch
                {
                    IAssignmentOperation a => a.Target,
                    IIncrementOrDecrementOperation i => i.Target,
                    _ => null,
                };
                if (IsConstructor(c.ContainingSymbol) || CallsRefresh(c.Operation, component))
                    return;
                if (target is IFieldReferenceOperation { Field: { IsStatic: false, IsReadOnly: false, IsImplicitlyDeclared: false } field }
                    && IsComponentMember(field, component))
                {
                    writes.Add((field, c.Operation.Syntax.GetLocation(), false));
                }
                else if (target is IPropertyReferenceOperation { Property: { IsStatic: false } prop }
                         && IsComponentMember(prop, component) && IsPlainAutoProperty(prop, stateAttr))
                {
                    writes.Add((prop, c.Operation.Syntax.GetLocation(), false));
                }
            }, OperationKind.SimpleAssignment, OperationKind.CompoundAssignment, OperationKind.CoalesceAssignment,
               OperationKind.Increment, OperationKind.Decrement);

            if (list is not null)
            {
                start.RegisterOperationAction(c =>
                {
                    var inv = (IInvocationOperation)c.Operation;
                    if (inv.Instance is not IFieldReferenceOperation { Field: { IsStatic: false } field })
                        return;
                    if (!s_listMutators.Contains(inv.TargetMethod.Name) || IsConstructor(c.ContainingSymbol) || CallsRefresh(inv, component))
                        return;
                    if (field.Type is INamedTypeSymbol t && SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, list)
                        && IsComponentMember(field, component))
                    {
                        writes.Add((field, inv.Syntax.GetLocation(), true));
                    }
                }, OperationKind.Invocation);
            }

            start.RegisterCompilationEndAction(end =>
            {
                foreach ((ISymbol? member, Location? location, bool isList) in writes)
                {
                    if (!readInView.ContainsKey(member))
                        continue;
                    ITypeSymbol type = member is IFieldSymbol f ? f.Type : ((IPropertySymbol)member).Type;
                    end.ReportDiagnostic(isList
                        ? Diagnostic.Create(UnobservedList, location, member.Name,
                            ((INamedTypeSymbol)type).TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
                        : Diagnostic.Create(UnobservedMember, location, member.Name,
                            type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), PascalCase(member.Name)));
                }
            });
        });
    }

    // MVU012: TextField(Name) or Checkbox(todo.Done) instead of Bind.Name or todo.Bind.Done. Reported when the control
    // accepts a binding and no change function is given: either the call does not compile (and the compiler message does
    // not say what to write), or it compiles and the control only displays the state.
    private static void CheckUnboundState(SyntaxNodeAnalysisContext c, INamedTypeSymbol stateAttr, INamedTypeSymbol binding)
    {
        var invocation = (InvocationExpressionSyntax)c.Node;
        SeparatedSyntaxList<ArgumentSyntax> args = invocation.ArgumentList.Arguments;
        if (args.Count == 0 || args[0].NameColon is not null)
            return;

        // Is the first argument a [State] property with a generated binding?
        SemanticModel model = c.SemanticModel;
        if (model.GetSymbolInfo(args[0].Expression, c.CancellationToken).Symbol is not IPropertySymbol state)
            return;
        if (state.SetMethod is not { IsInitOnly: false } || !state.ContainingType.IsReferenceType)
            return;
        if (!state.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, stateAttr)))
            return;

        // Does the control accept a binding as its first parameter?
        IEnumerable<IMethodSymbol> group = model.GetMemberGroup(invocation.Expression, c.CancellationToken).OfType<IMethodSymbol>();
        if (!group.Any(m => m.Parameters.Length > 0 && AcceptsBinding(m.Parameters[0].Type, state.Type, binding)))
            return;

        // A valid call is reported only when the change function (the second parameter, a delegate) is omitted. An
        // invalid call (TextField(Name) without a function) is always reported, to say what to write.
        if (model.GetOperation(invocation, c.CancellationToken) is IInvocationOperation call)
        {
            IArgumentOperation? change = call.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == 1);
            if (change?.Parameter is not { Type.TypeKind: TypeKind.Delegate } || change.ArgumentKind != ArgumentKind.DefaultValue)
                return;
        }

        SimpleNameSyntax? method = (invocation.Expression as MemberAccessExpressionSyntax)?.Name ?? invocation.Expression as SimpleNameSyntax;
        string suggestion = args[0].Expression is MemberAccessExpressionSyntax member
            ? $"{member.Expression}.Bind.{member.Name}"
            : $"Bind.{args[0].Expression}";
        c.ReportDiagnostic(Diagnostic.Create(UnboundState, args[0].GetLocation(),
            state.Name, method?.Identifier.Text ?? invocation.Expression.ToString(), suggestion));
    }

    // MVU013: Text("Title"); as a statement, outside a view builder block. The view is a description: unused, it is lost
    // without any error. Only a call or a creation makes a view; an assignment (node.View = view) has the type of a view
    // but keeps it.
    private static void CheckDiscardedView(OperationAnalysisContext c, INamedTypeSymbol view)
    {
        IOperation expression = ((IExpressionStatementOperation)c.Operation).Operation;
        string? name = expression switch
        {
            IInvocationOperation invocation when DerivesFrom(invocation.Type, view) => invocation.TargetMethod.Name,
            IObjectCreationOperation creation when DerivesFrom(creation.Type, view) => creation.Type!.Name,
            _ => null,
        };
        if (name is null)
            return;

        // In a view builder block, the generator intercepts the statement: the view joins the block.
        if (expression is IInvocationOperation { TargetMethod: { } method }
            && ViewBuilderSyntax.IsInterceptable(method)
            && ViewBuilderSyntax.IsBlockStatement(c.Operation.Syntax, c.Operation.SemanticModel!, c.CancellationToken))
        {
            return;
        }

        c.ReportDiagnostic(Diagnostic.Create(DiscardedView, c.Operation.Syntax.GetLocation(), name));
    }

    // MVU016: new CounterCard() in Body, or in a method that returns a view. A component created in an action, kept in a
    // field (_counter ??= new()), or returned to a cache by a lambda keeps its instance, so it is not reported.
    private static void CheckComponentCreatedInView(
        OperationAnalysisContext c, INamedTypeSymbol component, INamedTypeSymbol view, INamedTypeSymbol stateAttr)
    {
        var creation = (IObjectCreationOperation)c.Operation;
        if (creation.Type is not INamedTypeSymbol type || !DerivesFrom(type, component) || !DeclaresState(type, stateAttr))
            return;
        if (!IsViewMember(c.ContainingSymbol, view) || RunsOutsideTheView(creation, view) || IsKept(creation))
            return;

        c.ReportDiagnostic(Diagnostic.Create(ComponentCreatedInView, creation.Syntax.GetLocation(), type.Name));
    }

    // MVU017: .Task(async _ => await Task.Delay(1000)): nothing stops the task. Only a lambda that awaits something is
    // checked; a method group (.Task(LoadAsync)) is not analyzed.
    private static void CheckIgnoredCancellation(
        OperationAnalysisContext c, INamedTypeSymbol modifiers, INamedTypeSymbol componentModifiers, INamedTypeSymbol token)
    {
        var invocation = (IInvocationOperation)c.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        if (method.Name != TaskModifierName
            || !(SymbolEqualityComparer.Default.Equals(method.ContainingType, modifiers)
                || SymbolEqualityComparer.Default.Equals(method.ContainingType, componentModifiers)))
        {
            return;
        }

        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (argument.Value is not IDelegateCreationOperation { Target: IAnonymousFunctionOperation lambda })
                continue;
            ImmutableArray<IParameterSymbol> parameters = lambda.Symbol.Parameters;
            if (parameters.Length != 1 || !SymbolEqualityComparer.Default.Equals(parameters[0].Type, token))
                continue;

            IParameterSymbol parameter = parameters[0];

            bool awaits = lambda.Body.Descendants().OfType<IAwaitOperation>().Any();
            bool usesToken = lambda.Body.Descendants().OfType<IParameterReferenceOperation>()
                .Any(p => SymbolEqualityComparer.Default.Equals(p.Parameter, parameter));
            if (awaits && !usesToken)
                c.ReportDiagnostic(Diagnostic.Create(IgnoredCancellation, lambda.Syntax.GetLocation()));
        }
    }

    // Inside a lambda or a local function that does not build a view: an action, which runs later, or a factory, whose
    // result is kept by its caller.
    private static bool RunsOutsideTheView(IOperation op, INamedTypeSymbol view)
    {
        for (IOperation? p = op.Parent; p is not null; p = p.Parent)
        {
            IMethodSymbol? function = p switch
            {
                IAnonymousFunctionOperation lambda => lambda.Symbol,
                ILocalFunctionOperation local => local.Symbol,
                _ => null,
            };
            if (function is not null && !DerivesFrom(function.ReturnType, view))
                return true;
        }

        return false;
    }

    // Assigned to a field or a property (_counter ??= new CounterCard()): the instance is kept from one update to the next.
    private static bool IsKept(IOperation op)
    {
        IOperation? value = op;
        while (value.Parent is IConversionOperation conversion)
            value = conversion;
        return value.Parent is IAssignmentOperation { Target: IFieldReferenceOperation or IPropertyReferenceOperation } assignment
            && assignment.Value == value;
    }

    private static bool DeclaresState(INamedTypeSymbol type, INamedTypeSymbol stateAttr)
    {
        for (INamedTypeSymbol? t = type; t is not null; t = t.BaseType)
        {
            if (t.GetMembers().OfType<IPropertySymbol>().Any(p =>
                    p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, stateAttr))))
            {
                return true;
            }
        }

        return false;
    }

    private static bool DerivesFrom(ITypeSymbol? type, INamedTypeSymbol baseType)
    {
        for (ITypeSymbol? t = type; t is not null; t = t.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(t, baseType))
                return true;
        }

        return false;
    }

    // A Binding<T> parameter compatible with the type of the state (or generic, as in Segmented<T>).
    private static bool AcceptsBinding(ITypeSymbol parameter, ITypeSymbol stateType, INamedTypeSymbol binding) =>
        parameter is INamedTypeSymbol { IsGenericType: true } p
        && SymbolEqualityComparer.Default.Equals(p.OriginalDefinition, binding)
        && (p.TypeArguments[0] is ITypeParameterSymbol || SymbolEqualityComparer.Default.Equals(p.TypeArguments[0], stateType));

    private static void CheckStateDeclaration(SyntaxNodeAnalysisContext c, INamedTypeSymbol stateAttr)
    {
        var syntax = (PropertyDeclarationSyntax)c.Node;
        bool hasState = syntax.AttributeLists.SelectMany(l => l.Attributes).Any(a =>
            SymbolEqualityComparer.Default.Equals(
                (c.SemanticModel.GetSymbolInfo(a, c.CancellationToken).Symbol as IMethodSymbol)?.ContainingType, stateAttr));
        if (!hasState)
            return;

        Location location = syntax.Identifier.GetLocation();
        string name = syntax.Identifier.Text;

        if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            c.ReportDiagnostic(Diagnostic.Create(NotPartialProperty, location, name, syntax.Type.ToString()));
            return;
        }
        if (syntax.Modifiers.Any(SyntaxKind.StaticKeyword))
            c.ReportDiagnostic(Diagnostic.Create(InvalidShape, location, name, "cannot be static"));
        else if (syntax.AccessorList is null || syntax.AccessorList.Accessors.Any(a => a.Body is not null || a.ExpressionBody is not null))
            c.ReportDiagnostic(Diagnostic.Create(InvalidShape, location, name, "must not have accessor bodies because its implementation is generated"));
        else if (!syntax.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)))
            c.ReportDiagnostic(Diagnostic.Create(InvalidShape, location, name, "must have a 'get' accessor"));

        foreach (TypeDeclarationSyntax type in syntax.Ancestors().OfType<TypeDeclarationSyntax>())
        {
            if (!type.Modifiers.Any(SyntaxKind.PartialKeyword))
                c.ReportDiagnostic(Diagnostic.Create(NotPartialType, type.Identifier.GetLocation(), type.Identifier.Text));
        }
    }

    // MVU004: Bind or StateBindings declared by hand in a class that has settable state. The generator would then give up
    // writing the bindings, and the errors would appear elsewhere, with no apparent connection.
    private static void CheckReservedName(SyntaxNodeAnalysisContext c, INamedTypeSymbol stateAttr)
    {
        SyntaxToken[] names = c.Node switch
        {
            PropertyDeclarationSyntax p => [p.Identifier],
            MethodDeclarationSyntax m => [m.Identifier],
            EventDeclarationSyntax e => [e.Identifier],
            DelegateDeclarationSyntax d => [d.Identifier],
            BaseTypeDeclarationSyntax t => [t.Identifier],
            BaseFieldDeclarationSyntax f => [.. f.Declaration.Variables.Select(v => v.Identifier)],
            _ => [],
        };

        foreach (SyntaxToken name in names.Where(n => s_reservedNames.Contains(n.ValueText)))
        {
            if (c.SemanticModel.GetDeclaredSymbol(c.Node is BaseFieldDeclarationSyntax ? name.Parent! : c.Node, c.CancellationToken)
                    ?.ContainingType is not { } owner)
            {
                continue;
            }

            if (!HasBindableState(owner, stateAttr))
                continue;
            c.ReportDiagnostic(Diagnostic.Create(ReservedName, name.GetLocation(), name.ValueText, owner.Name));
        }
    }

    // The class has at least one settable state: the generator writes Bind and StateBindings in it.
    private static bool HasBindableState(INamedTypeSymbol type, INamedTypeSymbol stateAttr) =>
        type.IsReferenceType && type.GetMembers().OfType<IPropertySymbol>().Any(p =>
            p.SetMethod is { IsInitOnly: false }
            && p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, stateAttr)));

    private static bool IsComponentMember(ISymbol member, INamedTypeSymbol component)
    {
        for (INamedTypeSymbol? t = member.ContainingType; t is not null; t = t.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(t, component))
                return true;
        }

        return false;
    }

    // The Body of a component, or a method that returns a view (Row, Header…).
    private static bool IsViewMember(ISymbol symbol, INamedTypeSymbol view)
    {
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } m)
            symbol = m.ContainingSymbol;
        if (symbol is not IMethodSymbol method)
            return false;
        if (method.AssociatedSymbol is IPropertySymbol { Name: "Body" })
            return true;
        for (ITypeSymbol? t = method.ReturnType; t is not null; t = t.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(t, view))
                return true;
        }

        return false;
    }

    private static bool IsConstructor(ISymbol symbol) => symbol is IMethodSymbol { MethodKind: MethodKind.Constructor };

    // The method that changes the state calls Refresh(): a deliberate manual update.
    private static bool CallsRefresh(IOperation op, INamedTypeSymbol component)
    {
        IOperation root = op;
        while (root.Parent is not null)
            root = root.Parent;
        return root.Descendants().OfType<IInvocationOperation>().Any(i =>
            i.TargetMethod.Name == "Refresh" && SymbolEqualityComparer.Default.Equals(i.TargetMethod.ContainingType, component));
    }

    private static bool IsWriteTarget(IOperation op) => op.Parent switch
    {
        IAssignmentOperation a => a.Target == op,
        IIncrementOrDecrementOperation => true,
        _ => false,
    };

    private static bool InsideLambda(IOperation op)
    {
        for (IOperation? p = op.Parent; p is not null; p = p.Parent)
        {
            if (p is IAnonymousFunctionOperation)
                return true;
        }

        return false;
    }

    private static bool IsPlainAutoProperty(IPropertySymbol prop, INamedTypeSymbol stateAttr)
    {
        return !prop.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, stateAttr))
            && prop.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<PropertyDeclarationSyntax>().Any(p =>
            p.AccessorList is { } accessors && accessors.Accessors.All(a => a.Body is null && a.ExpressionBody is null)
            && !p.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static string PascalCase(string name)
    {
        name = name.TrimStart('_');
        return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
}
