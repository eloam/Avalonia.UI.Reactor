// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.UI.Reactor.Generators;

/// <summary>
/// Provides the rules of view builder blocks shared by the generator, which intercepts their view statements, and by the
/// analyzer, which must not report these statements as views never displayed.
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
    /// Determines whether a method can be intercepted in a block: it returns a view, it is not the invocation of a
    /// delegate, and the generated code can call it.
    /// </summary>
    /// <param name="method">The method called by the statement.</param>
    /// <returns><see langword="true"/> if the call can be intercepted; otherwise, <see langword="false"/>.</returns>
    public static bool IsInterceptable(IMethodSymbol method)
    {
        IMethodSymbol definition = (method.ReducedFrom ?? method).OriginalDefinition;
        if (definition.MethodKind != MethodKind.Ordinary || !ReturnsView(definition.ReturnType))
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

    // The parameter of the lambda comes from the semantic model, so that a named argument (content: () => { … }) is
    // found wherever it is written. A block converts to a delegate that returns nothing: with a Func<…> overload,
    // () => Text("a") is a value, not a block.
    private static bool IsBlockLambda(AnonymousFunctionExpressionSyntax lambda, SemanticModel model, CancellationToken cancellation)
    {
        if (lambda.Parent is not ArgumentSyntax argument
            || model.GetOperation(argument, cancellation) is not IArgumentOperation { Parameter: { } parameter }
            || parameter.Type is not INamedTypeSymbol { DelegateInvokeMethod.ReturnsVoid: true })
        {
            return false;
        }

        return parameter.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == AttributeName);
    }

    private static bool ReturnsView(ITypeSymbol type)
    {
        for (ITypeSymbol? t = type; t is not null; t = t.BaseType)
        {
            if (t.ToDisplayString() == ViewTypeName)
                return true;
        }

        // A type parameter constrained to a view (T : View), as the modifiers return.
        return type is ITypeParameterSymbol parameter && parameter.ConstraintTypes.Any(ReturnsView);
    }

    private static bool IsAssemblyVisible(Accessibility accessibility) =>
        accessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;
}
