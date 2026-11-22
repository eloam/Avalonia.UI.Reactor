// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.UI.Reactor.Generators;

/// <summary>
/// Provides the rules of view builder blocks shared by the generator, which intercepts their view statements, the
/// analyzers, which check their statements, and the code fixes.
/// </summary>
internal static class ViewBuilderSyntax
{
    /// <summary>
    /// The full name of the attribute that marks a parameter receiving a block.
    /// </summary>
    public const string AttributeName = "Avalonia.UI.Reactor.Views.ViewBuilderAttribute";

    /// <summary>
    /// The full name of the base type of views.
    /// </summary>
    public const string ViewTypeName = "Avalonia.UI.Reactor.Views.View";

    /// <summary>
    /// The full name of the base type of components, which convert to views.
    /// </summary>
    public const string ComponentTypeName = "Avalonia.UI.Reactor.State.Component";

    /// <summary>
    /// The full name of the type of the view factories.
    /// </summary>
    public const string FactoriesTypeName = "Avalonia.UI.Reactor.Views.Ui";

    /// <summary>
    /// The name of the factory that adds any view to a block.
    /// </summary>
    public const string ShowName = "Show";

    /// <summary>
    /// Finds, from the syntax only, the lambda whose block would contain a statement.
    /// </summary>
    /// <remarks>
    /// A statement of a block is a statement whose ancestors, up to a lambda passed as an argument, are only blocks,
    /// <see langword="if"/> and <see langword="else"/>, and <see langword="switch"/> sections; or the body of such a
    /// lambda written without braces. A statement in a loop, a <see langword="try"/> or a local function is not one.
    /// Whether the lambda is a block depends on the parameter it is passed to (see <see cref="IsBlockStatement"/>).
    /// </remarks>
    /// <param name="node">A statement, or the expression that is the body of a lambda.</param>
    /// <returns>The lambda passed as an argument that contains the statement, or <see langword="null"/> if the
    /// statement cannot be part of a block.</returns>
    public static AnonymousFunctionExpressionSyntax? BlockLambdaOf(SyntaxNode node)
    {
        if (node is ExpressionSyntax { Parent: LambdaExpressionSyntax body } expression && body.ExpressionBody == expression)
            return body.Parent is ArgumentSyntax ? body : null;
        if (node is not StatementSyntax statement)
            return null;

        for (SyntaxNode? parent = statement.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case BlockSyntax or IfStatementSyntax or ElseClauseSyntax or SwitchSectionSyntax or SwitchStatementSyntax:
                    continue;
                case AnonymousFunctionExpressionSyntax lambda:
                    return lambda.Parent is ArgumentSyntax ? lambda : null;
                default:
                    return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the lambda whose code contains a node, at any depth of statements, without crossing a local function.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The innermost lambda that contains the node, or <see langword="null"/> if the node is in a local
    /// function or directly in a member.</returns>
    public static AnonymousFunctionExpressionSyntax? EnclosingLambda(SyntaxNode node)
    {
        for (SyntaxNode? parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case AnonymousFunctionExpressionSyntax lambda:
                    return lambda;
                case LocalFunctionStatementSyntax or MemberDeclarationSyntax:
                    return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a node is a statement of a view builder block.
    /// </summary>
    /// <remarks>
    /// The statement is placed as <see cref="BlockLambdaOf"/> describes, in a lambda passed to a <c>[ViewBuilder]</c>
    /// parameter. A statement of another lambda (an action) is not one.
    /// </remarks>
    /// <param name="node">A statement, or the expression that is the body of a lambda.</param>
    /// <param name="model">The semantic model of the tree.</param>
    /// <param name="cancellation">The cancellation of the analysis.</param>
    /// <returns><see langword="true"/> if the node is a statement of a block; otherwise, <see langword="false"/>.</returns>
    public static bool IsBlockStatement(SyntaxNode node, SemanticModel model, CancellationToken cancellation) =>
        BlockLambdaOf(node) is { } lambda && IsBlockLambda(lambda, model, cancellation);

    /// <summary>
    /// Determines whether a lambda is a view builder block: it is passed to a <c>[ViewBuilder]</c> parameter.
    /// </summary>
    /// <remarks>
    /// The parameter comes from the semantic model, so that a named argument (<c>content: () =&gt; { … }</c>) is found
    /// wherever it is written. A block converts to a delegate that returns nothing: with a <c>Func&lt;…&gt;</c>
    /// overload, <c>() =&gt; Text("a")</c> is a value, not a block.
    /// </remarks>
    /// <param name="lambda">The lambda.</param>
    /// <param name="model">The semantic model of the tree.</param>
    /// <param name="cancellation">The cancellation of the analysis.</param>
    /// <returns><see langword="true"/> if the lambda is a block; otherwise, <see langword="false"/>.</returns>
    public static bool IsBlockLambda(AnonymousFunctionExpressionSyntax lambda, SemanticModel model, CancellationToken cancellation) =>
        lambda.Parent is ArgumentSyntax argument
        && model.GetOperation(argument, cancellation) is IArgumentOperation { Parameter: { } parameter }
        && IsBlockParameter(parameter);

    /// <summary>
    /// Determines whether a lambda may be a view builder block while the call that receives it does not resolve, for
    /// example because the block contains an error that a code fix corrects.
    /// </summary>
    /// <param name="lambda">The lambda.</param>
    /// <param name="model">The semantic model of the tree.</param>
    /// <param name="cancellation">The cancellation of the analysis.</param>
    /// <returns><see langword="true"/> if the lambda is a block, or if one of the candidate methods of the call takes a
    /// block in its place; otherwise, <see langword="false"/>.</returns>
    public static bool MayBeBlockLambda(AnonymousFunctionExpressionSyntax lambda, SemanticModel model, CancellationToken cancellation)
    {
        if (IsBlockLambda(lambda, model, cancellation))
            return true;
        if (lambda.Parent is not ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } list } argument)
            return false;

        int position = list.Arguments.IndexOf(argument);
        return model.GetSymbolInfo(invocation, cancellation).CandidateSymbols.OfType<IMethodSymbol>().Any(method =>
        {
            IParameterSymbol? parameter = argument.NameColon is { } name
                ? method.Parameters.FirstOrDefault(p => p.Name == name.Name.Identifier.ValueText)
                : position < method.Parameters.Length ? method.Parameters[position] : null;
            return parameter is not null && IsBlockParameter(parameter);
        });
    }

    /// <summary>
    /// Determines whether a method can be intercepted in a block: it returns a view, it is not the invocation of a
    /// delegate, and the generated code can call it.
    /// </summary>
    /// <param name="method">The method called by the statement.</param>
    /// <returns><see langword="true"/> if the call can be intercepted; otherwise, <see langword="false"/>.</returns>
    public static bool IsInterceptable(IMethodSymbol method)
    {
        IMethodSymbol definition = (method.ReducedFrom ?? method).OriginalDefinition;
        if (definition.MethodKind != MethodKind.Ordinary || !IsView(definition.ReturnType))
            return false;
        if (definition.Parameters.Any(p => p.RefKind != RefKind.None) || definition.ContainingType.IsGenericType)
            return false;

        // An instance method of a struct would be called on a copy of it.
        if (!definition.IsStatic && definition.ContainingType.IsValueType)
            return false;

        // A private generic method would need a generic UnsafeAccessor; it is not intercepted.
        return !definition.IsGenericMethod || IsAccessibleFromAssembly(definition);
    }

    /// <summary>
    /// Determines whether the generated code, in the same assembly, can call a method directly.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns><see langword="true"/> if the method and its types are public or internal; otherwise,
    /// <see langword="false"/>.</returns>
    public static bool IsAccessibleFromAssembly(IMethodSymbol method)
    {
        if (!IsAssemblyVisible(method.DeclaredAccessibility))
            return false;

        for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            if (!IsAssemblyVisible(type.DeclaredAccessibility))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether a method is <c>Ui.Show</c>, which adds any view to a block.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns><see langword="true"/> if the method is <c>Ui.Show</c>; otherwise, <see langword="false"/>.</returns>
    public static bool IsShow(IMethodSymbol method) =>
        method.Name == ShowName && method.ContainingType.ToDisplayString() == FactoriesTypeName;

    /// <summary>
    /// Determines whether a type is a view: a type derived from <c>View</c>, or a type parameter constrained to it, as
    /// the modifiers return.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><see langword="true"/> if the type is a view; otherwise, <see langword="false"/>.</returns>
    public static bool IsView(ITypeSymbol? type) =>
        DerivesFrom(type, ViewTypeName) || (type is ITypeParameterSymbol parameter && parameter.ConstraintTypes.Any(IsView));

    /// <summary>
    /// Determines whether a type is a view or a component, which converts to a view.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><see langword="true"/> if the type is a view or a component; otherwise, <see langword="false"/>.</returns>
    public static bool IsViewOrComponent(ITypeSymbol? type) => IsView(type) || DerivesFrom(type, ComponentTypeName);

    /// <summary>
    /// Determines whether a parameter receives a block: it is marked <c>[ViewBuilder]</c>, and its delegate returns
    /// nothing.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns><see langword="true"/> if the parameter receives a block; otherwise, <see langword="false"/>.</returns>
    public static bool IsBlockParameter(IParameterSymbol parameter) =>
        parameter.Type is INamedTypeSymbol { DelegateInvokeMethod.ReturnsVoid: true }
        && parameter.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == AttributeName);

    private static bool DerivesFrom(ITypeSymbol? type, string baseTypeName)
    {
        for (ITypeSymbol? t = type; t is not null; t = t.BaseType)
        {
            if (t.ToDisplayString() == baseTypeName)
                return true;
        }

        return false;
    }

    private static bool IsAssemblyVisible(Accessibility accessibility) =>
        accessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;
}
