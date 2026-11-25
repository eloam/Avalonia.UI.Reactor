// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace MartletUI.Generators;

/// <summary>
/// Finds the operations that change state while a view is computed, for the analyzers (MVU018).
/// </summary>
internal static class StateChange
{
    private const string StateAttributeName = "MartletUI.State.StateAttribute";
    private const string StateListTypeName = "MartletUI.State.StateList<T>";
    private const string BindingTypeName = "MartletUI.State.Binding<T>";
    private const string BindingSetName = "Set";
    private const string RefreshName = "Refresh";

    // The methods of StateList<T> that change the list.
    private static readonly ImmutableHashSet<string> s_listMutators = ImmutableHashSet.Create(
        "Add", "AddRange", "Insert", "Remove", "RemoveAt", "RemoveAll", "Clear", "Move");

    /// <summary>
    /// Gets the kinds of operations that can change state.
    /// </summary>
    public static ImmutableArray<OperationKind> Kinds { get; } =
    [
        OperationKind.SimpleAssignment, OperationKind.CompoundAssignment, OperationKind.CoalesceAssignment,
        OperationKind.DeconstructionAssignment, OperationKind.Increment, OperationKind.Decrement, OperationKind.Invocation,
    ];

    /// <summary>
    /// Finds the state that an operation changes while a view is computed.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <param name="member">The symbol that contains the operation.</param>
    /// <param name="cancellation">The cancellation of the analysis.</param>
    /// <returns>The names of the changed states, empty if the operation changes none or runs outside the view, and
    /// whether the operation is a call to <c>Refresh</c>.</returns>
    public static (ImmutableArray<string> Names, bool IsRefresh) InView(
        IOperation operation, ISymbol member, CancellationToken cancellation)
    {
        ImmutableArray<string> names = [.. NamesOf(operation)];
        bool refresh = IsRefresh(operation);
        return (names.IsEmpty && !refresh) || !IsComputedWithView(operation, member, cancellation)
            ? (ImmutableArray<string>.Empty, false)
            : (names, refresh);
    }

    private static IEnumerable<string> NamesOf(IOperation operation)
    {
        IEnumerable<IOperation> targets = operation switch
        {
            IDeconstructionAssignmentOperation { Target: ITupleOperation tuple } => tuple.Elements,
            IAssignmentOperation assignment => [assignment.Target],
            IIncrementOrDecrementOperation increment => [increment.Target],
            _ => [],
        };
        foreach (IPropertyReferenceOperation target in targets.OfType<IPropertyReferenceOperation>())
        {
            if (IsState(target.Property) && !IsNew(target.Instance))
                yield return target.Property.Name;
            else if (target.Property.IsIndexer && IsStateList(target.Instance?.Type) && !IsNew(target.Instance))
                yield return NameOf(target.Instance);
        }

        if (operation is IInvocationOperation { Instance: { } instance, TargetMethod: { } method }
            && ((s_listMutators.Contains(method.Name) && IsStateList(instance.Type) && !IsNew(instance))
                || (method.Name == BindingSetName && IsDefinedBy(instance.Type, BindingTypeName))))
        {
            yield return NameOf(instance);
        }
    }

    private static bool IsRefresh(IOperation operation) =>
        operation is IInvocationOperation { TargetMethod: { Name: RefreshName } method }
        && method.ContainingType.ToDisplayString() == ViewBuilderSyntax.ComponentTypeName;

    private static bool IsState(IPropertySymbol property) =>
        property.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == StateAttributeName);

    private static bool IsStateList(ITypeSymbol? type) => IsDefinedBy(type, StateListTypeName);

    private static bool IsDefinedBy(ITypeSymbol? type, string definition) => type?.OriginalDefinition.ToDisplayString() == definition;

    private static string NameOf(IOperation? instance) => instance switch
    {
        IMemberReferenceOperation member => member.Member.Name,
        ILocalReferenceOperation local => local.Local.Name,
        IParameterReferenceOperation parameter => parameter.Parameter.Name,
        _ => instance?.Type?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? string.Empty,
    };

    // The state of an object created by the view itself, in its initializer or through the variable that holds it:
    // nothing depends on it yet.
    private static bool IsNew(IOperation? instance)
    {
        switch (instance)
        {
            case IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ImplicitReceiver }:
            case IObjectCreationOperation:
                return true;
            case ILocalReferenceOperation { Local: { } local }:
                return local.DeclaringSyntaxReferences.Any(reference =>
                    reference.GetSyntax() is VariableDeclaratorSyntax
                    {
                        Initializer.Value: BaseObjectCreationExpressionSyntax or CollectionExpressionSyntax
                    });
            default:
                return false;
        }
    }

    // In a view builder block, or in a member that returns a view (Body, Header…). A lambda or a local
    // function that does not return a view is an action, which runs later.
    private static bool IsComputedWithView(IOperation operation, ISymbol member, CancellationToken cancellation)
    {
        for (IOperation? parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case IAnonymousFunctionOperation { Syntax: AnonymousFunctionExpressionSyntax syntax } lambda:
                    if (ViewBuilderSyntax.IsBlockLambda(syntax, lambda.SemanticModel!, cancellation))
                        return true;
                    if (!ViewBuilderSyntax.IsView(lambda.Symbol.ReturnType))
                        return false;
                    break;
                case ILocalFunctionOperation local when !ViewBuilderSyntax.IsView(local.Symbol.ReturnType):
                    return false;
            }
        }

        while (member is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } function)
            member = function.ContainingSymbol;
        return member is IMethodSymbol method && ViewBuilderSyntax.IsView(method.ReturnType);
    }
}
