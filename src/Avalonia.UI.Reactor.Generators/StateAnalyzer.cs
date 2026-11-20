using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.UI.Reactor.Generators;

/// <summary>
/// Signale les pièges de l'état réactif à la compilation :
/// une propriété [State] mal déclarée (MVU001–003), un état affiché par la vue
/// mais modifié sans être observé — l'interface ne se mettrait pas à jour (MVU010–011),
/// ou un état passé à un contrôle sans sa liaison — le contrôle ne le modifierait pas (MVU012).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StateAnalyzer : DiagnosticAnalyzer
{
    // Messages au format des analyseurs Microsoft : titre court sans point final, message d'une phrase sans
    // point final, noms entre apostrophes, description en phrases complètes (pourquoi + comment corriger).
    const string Category = "Usage";

    public static readonly DiagnosticDescriptor NotPartialProperty = new(
        "MVU001",
        title: "State property must be partial",
        messageFormat: "State property '{0}' must be declared 'partial', for example '[State] partial {1} {0} {{ get; set; }}'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "The implementation of a [State] property is generated. Declare the property as 'partial', with accessors that have no body.");

    public static readonly DiagnosticDescriptor NotPartialType = new(
        "MVU002",
        title: "Type declaring state properties must be partial",
        messageFormat: "Type '{0}' must be declared 'partial' because it contains [State] properties",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "The generator adds members to the type that declares [State] properties and to each of its containing types, so all of them must be partial.");

    public static readonly DiagnosticDescriptor InvalidShape = new(
        "MVU003",
        title: "Invalid state property declaration",
        messageFormat: "State property '{0}' {1}",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "A [State] property must be an instance property with a 'get' accessor, and its accessors must not have a body because its implementation is generated.");

    public static readonly DiagnosticDescriptor ReservedName = new(
        "MVU004",
        title: "Name reserved for generated bindings",
        messageFormat: "'{0}' is reserved for the bindings generated for the [State] properties of '{1}'; rename this member",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "The generator adds a 'Bind' property and a 'StateBindings' type to every class that declares settable [State] properties. A member with one of these names prevents the bindings from being generated.");

    /// <summary>Noms écrits par le générateur dans chaque classe qui a des états modifiables.</summary>
    static readonly ImmutableHashSet<string> ReservedNames = ImmutableHashSet.Create("Bind", "StateBindings");

    public static readonly DiagnosticDescriptor UnobservedMember = new(
        "MVU010",
        title: "Displayed member is not observable",
        messageFormat: "'{0}' is read by the view but is not observable, so changing it does not update the UI; declare it as '[State] partial {1} {2} {{ get; set; }}' or call 'Refresh()'",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        description: "A field or auto-property that a view reads (in 'Body' or in a method that returns a 'View') and that is modified elsewhere does not notify the view. Make it a [State] property, or call 'Refresh()' after changing it.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor UnobservedList = new(
        "MVU011",
        title: "Displayed list is not observable",
        messageFormat: "List '{0}' is read by the view but is not observable, so changing it does not update the UI; use 'StateList<{1}>'",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        description: "Adding, removing or replacing the items of a 'List<T>' does not notify the view. Use 'StateList<T>', which notifies the views that enumerate it.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor UnboundState = new(
        "MVU012",
        title: "State property passed without its binding",
        messageFormat: "State property '{0}' is passed by value, so '{1}' cannot update it; pass '{2}' instead",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "A control that supports two-way binding only displays a [State] property that is passed by value. Pass its generated binding ('Bind.Name', or 'item.Bind.Name' for a model object) so that the control can update it. To make the control read-only on purpose, pass 'null' as the change handler.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(NotPartialProperty, NotPartialType, InvalidShape, ReservedName, UnobservedMember, UnobservedList, UnboundState);

    static readonly ImmutableHashSet<string> ListMutators = ImmutableHashSet.Create(
        "Add", "AddRange", "Insert", "InsertRange", "Remove", "RemoveAt", "RemoveAll", "RemoveRange", "Clear", "Sort", "Reverse");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            var stateAttr = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.State.StateAttribute");
            var component = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.State.Component");
            var view = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.Views.View");
            var list = start.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            if (stateAttr is null || component is null || view is null) return;

            start.RegisterSyntaxNodeAction(c => CheckStateDeclaration(c, stateAttr), SyntaxKind.PropertyDeclaration);

            // Membres écrits à la main sous un nom réservé aux liaisons générées (le code généré n'est pas analysé).
            start.RegisterSyntaxNodeAction(c => CheckReservedName(c, stateAttr),
                SyntaxKind.PropertyDeclaration, SyntaxKind.MethodDeclaration, SyntaxKind.FieldDeclaration,
                SyntaxKind.EventDeclaration, SyntaxKind.EventFieldDeclaration, SyntaxKind.DelegateDeclaration,
                SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration,
                SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration, SyntaxKind.EnumDeclaration);

            var binding = start.Compilation.GetTypeByMetadataName("Avalonia.UI.Reactor.State.Binding`1");
            if (binding is not null)
                start.RegisterSyntaxNodeAction(c => CheckUnboundState(c, stateAttr, binding), SyntaxKind.InvocationExpression);

            // Membres lus par une vue / modifiés ailleurs : rapprochés en fin de compilation.
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
                if (member is null || member.IsStatic || !IsComponentMember(member, component)) return;
                if (IsWriteTarget(c.Operation) || InsideLambda(c.Operation)) return;
                if (IsViewMember(c.ContainingSymbol, view)) readInView.TryAdd(member, 0);
            }, OperationKind.FieldReference, OperationKind.PropertyReference);

            start.RegisterOperationAction(c =>
            {
                var target = c.Operation switch
                {
                    IAssignmentOperation a => a.Target,
                    IIncrementOrDecrementOperation i => i.Target,
                    _ => null,
                };
                if (IsConstructor(c.ContainingSymbol) || CallsRefresh(c.Operation, component)) return;
                if (target is IFieldReferenceOperation { Field: { IsStatic: false, IsReadOnly: false, IsImplicitlyDeclared: false } field }
                    && IsComponentMember(field, component))
                    writes.Add((field, c.Operation.Syntax.GetLocation(), false));
                else if (target is IPropertyReferenceOperation { Property: { IsStatic: false } prop }
                         && IsComponentMember(prop, component) && IsPlainAutoProperty(prop, stateAttr))
                    writes.Add((prop, c.Operation.Syntax.GetLocation(), false));
            }, OperationKind.SimpleAssignment, OperationKind.CompoundAssignment, OperationKind.CoalesceAssignment,
               OperationKind.Increment, OperationKind.Decrement);

            if (list is not null)
                start.RegisterOperationAction(c =>
                {
                    var inv = (IInvocationOperation)c.Operation;
                    if (inv.Instance is not IFieldReferenceOperation { Field: { IsStatic: false } field }) return;
                    if (!ListMutators.Contains(inv.TargetMethod.Name) || IsConstructor(c.ContainingSymbol) || CallsRefresh(inv, component)) return;
                    if (field.Type is INamedTypeSymbol t && SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, list)
                        && IsComponentMember(field, component))
                        writes.Add((field, inv.Syntax.GetLocation(), true));
                }, OperationKind.Invocation);

            start.RegisterCompilationEndAction(end =>
            {
                foreach (var (member, location, isList) in writes)
                {
                    if (!readInView.ContainsKey(member)) continue;
                    var type = member is IFieldSymbol f ? f.Type : ((IPropertySymbol)member).Type;
                    end.ReportDiagnostic(isList
                        ? Diagnostic.Create(UnobservedList, location, member.Name,
                            ((INamedTypeSymbol)type).TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
                        : Diagnostic.Create(UnobservedMember, location, member.Name,
                            type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), PascalCase(member.Name)));
                }
            });
        });
    }

    /// <summary>
    /// MVU012 : <c>TextField(Name)</c> ou <c>Checkbox(todo.Done)</c> au lieu de <c>Bind.Name</c> / <c>todo.Bind.Done</c>.
    /// Signalé quand le contrôle accepte une liaison et qu'aucune fonction de modification n'est donnée : soit l'appel
    /// ne compile pas (le message du compilateur ne dit pas quoi faire), soit il compile et le contrôle ne fait qu'afficher l'état.
    /// </summary>
    static void CheckUnboundState(SyntaxNodeAnalysisContext c, INamedTypeSymbol stateAttr, INamedTypeSymbol binding)
    {
        var invocation = (InvocationExpressionSyntax)c.Node;
        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0 || args[0].NameColon is not null) return;

        // Le premier argument est-il un état [State] qui a une liaison générée ?
        var model = c.SemanticModel;
        if (model.GetSymbolInfo(args[0].Expression, c.CancellationToken).Symbol is not IPropertySymbol state) return;
        if (state.SetMethod is not { IsInitOnly: false } || !state.ContainingType.IsReferenceType) return;
        if (!state.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, stateAttr))) return;

        // Le contrôle accepte-t-il une liaison en premier paramètre ?
        var group = model.GetMemberGroup(invocation.Expression, c.CancellationToken).OfType<IMethodSymbol>();
        if (!group.Any(m => m.Parameters.Length > 0 && AcceptsBinding(m.Parameters[0].Type, state.Type, binding))) return;

        // Appel valide : ne signaler que si la fonction de modification (2e paramètre, un délégué) est omise.
        // Appel invalide (ex. TextField(Name) sans fonction) : toujours signaler, pour dire quoi écrire.
        if (model.GetOperation(invocation, c.CancellationToken) is IInvocationOperation call)
        {
            var change = call.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == 1);
            if (change?.Parameter is not { Type.TypeKind: TypeKind.Delegate } || change.ArgumentKind != ArgumentKind.DefaultValue) return;
        }

        var method = (invocation.Expression as MemberAccessExpressionSyntax)?.Name ?? invocation.Expression as SimpleNameSyntax;
        string suggestion = args[0].Expression is MemberAccessExpressionSyntax member
            ? $"{member.Expression}.Bind.{member.Name}"
            : $"Bind.{args[0].Expression}";
        c.ReportDiagnostic(Diagnostic.Create(UnboundState, args[0].GetLocation(),
            state.Name, method?.Identifier.Text ?? invocation.Expression.ToString(), suggestion));
    }

    /// <summary>Paramètre <c>Binding&lt;T&gt;</c> compatible avec le type de l'état (ou générique, comme <c>Segmented&lt;T&gt;</c>).</summary>
    static bool AcceptsBinding(ITypeSymbol parameter, ITypeSymbol stateType, INamedTypeSymbol binding) =>
        parameter is INamedTypeSymbol { IsGenericType: true } p
        && SymbolEqualityComparer.Default.Equals(p.OriginalDefinition, binding)
        && (p.TypeArguments[0] is ITypeParameterSymbol || SymbolEqualityComparer.Default.Equals(p.TypeArguments[0], stateType));

    static void CheckStateDeclaration(SyntaxNodeAnalysisContext c, INamedTypeSymbol stateAttr)
    {
        var syntax = (PropertyDeclarationSyntax)c.Node;
        bool hasState = syntax.AttributeLists.SelectMany(l => l.Attributes).Any(a =>
            SymbolEqualityComparer.Default.Equals(
                (c.SemanticModel.GetSymbolInfo(a, c.CancellationToken).Symbol as IMethodSymbol)?.ContainingType, stateAttr));
        if (!hasState) return;

        var location = syntax.Identifier.GetLocation();
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

        foreach (var type in syntax.Ancestors().OfType<TypeDeclarationSyntax>())
            if (!type.Modifiers.Any(SyntaxKind.PartialKeyword))
                c.ReportDiagnostic(Diagnostic.Create(NotPartialType, type.Identifier.GetLocation(), type.Identifier.Text));
    }

    /// <summary>
    /// MVU004 : <c>Bind</c> ou <c>StateBindings</c> déclaré à la main dans une classe qui a des états modifiables.
    /// Le générateur renoncerait alors à écrire les liaisons, et les erreurs apparaîtraient ailleurs, sans rapport apparent.
    /// </summary>
    static void CheckReservedName(SyntaxNodeAnalysisContext c, INamedTypeSymbol stateAttr)
    {
        var names = c.Node switch
        {
            PropertyDeclarationSyntax p => [p.Identifier],
            MethodDeclarationSyntax m => [m.Identifier],
            EventDeclarationSyntax e => [e.Identifier],
            DelegateDeclarationSyntax d => [d.Identifier],
            BaseTypeDeclarationSyntax t => [t.Identifier],
            BaseFieldDeclarationSyntax f => f.Declaration.Variables.Select(v => v.Identifier).ToArray(),
            _ => System.Array.Empty<SyntaxToken>(),
        };

        foreach (var name in names.Where(n => ReservedNames.Contains(n.ValueText)))
        {
            if (c.SemanticModel.GetDeclaredSymbol(c.Node is BaseFieldDeclarationSyntax ? name.Parent! : c.Node, c.CancellationToken)
                    ?.ContainingType is not { } owner)
                continue;
            if (!HasBindableState(owner, stateAttr)) continue;
            c.ReportDiagnostic(Diagnostic.Create(ReservedName, name.GetLocation(), name.ValueText, owner.Name));
        }
    }

    /// <summary>La classe a au moins un état modifiable : le générateur lui écrit <c>Bind</c> et <c>StateBindings</c>.</summary>
    static bool HasBindableState(INamedTypeSymbol type, INamedTypeSymbol stateAttr) =>
        type.IsReferenceType && type.GetMembers().OfType<IPropertySymbol>().Any(p =>
            p.SetMethod is { IsInitOnly: false }
            && p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, stateAttr)));

    static bool IsComponentMember(ISymbol member, INamedTypeSymbol component)
    {
        for (var t = member.ContainingType; t is not null; t = t.BaseType)
            if (SymbolEqualityComparer.Default.Equals(t, component)) return true;
        return false;
    }

    /// <summary>Le Body d'un composant, ou une méthode qui renvoie une vue (Row, Header…).</summary>
    static bool IsViewMember(ISymbol symbol, INamedTypeSymbol view)
    {
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } m)
            symbol = m.ContainingSymbol;
        if (symbol is not IMethodSymbol method) return false;
        if (method.AssociatedSymbol is IPropertySymbol { Name: "Body" }) return true;
        for (var t = method.ReturnType; t is not null; t = t.BaseType)
            if (SymbolEqualityComparer.Default.Equals(t, view)) return true;
        return false;
    }

    static bool IsConstructor(ISymbol symbol) => symbol is IMethodSymbol { MethodKind: MethodKind.Constructor };

    /// <summary>La méthode qui modifie l'état appelle Refresh() : mise à jour manuelle assumée.</summary>
    static bool CallsRefresh(IOperation op, INamedTypeSymbol component)
    {
        var root = op;
        while (root.Parent is not null) root = root.Parent;
        return root.Descendants().OfType<IInvocationOperation>().Any(i =>
            i.TargetMethod.Name == "Refresh" && SymbolEqualityComparer.Default.Equals(i.TargetMethod.ContainingType, component));
    }

    static bool IsWriteTarget(IOperation op) => op.Parent switch
    {
        IAssignmentOperation a => a.Target == op,
        IIncrementOrDecrementOperation => true,
        _ => false,
    };

    static bool InsideLambda(IOperation op)
    {
        for (var p = op.Parent; p is not null; p = p.Parent)
            if (p is IAnonymousFunctionOperation) return true;
        return false;
    }

    static bool IsPlainAutoProperty(IPropertySymbol prop, INamedTypeSymbol stateAttr)
    {
        if (prop.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, stateAttr))) return false;
        return prop.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<PropertyDeclarationSyntax>().Any(p =>
            p.AccessorList is { } accessors && accessors.Accessors.All(a => a.Body is null && a.ExpressionBody is null)
            && !p.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    static string PascalCase(string name)
    {
        name = name.TrimStart('_');
        return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
}
