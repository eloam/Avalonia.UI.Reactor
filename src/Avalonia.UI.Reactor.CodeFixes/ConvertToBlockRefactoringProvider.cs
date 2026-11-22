// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.UI.Reactor.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

namespace Avalonia.UI.Reactor.CodeFixes;

/// <summary>
/// Provides the refactoring "Convert to block" for a container whose children are arguments, one of them conditional.
/// </summary>
/// <remarks>
/// The usage rule of the framework: children as arguments for a fixed list of views, a block as soon as there is a
/// condition. <c>VStack(Header(), ShowAbout ? About() : null, _todos)</c> becomes a block where the conditional is
/// an <see langword="if"/>, a view call a statement, and any other view an argument of <c>Show</c>. The refactoring is
/// offered on a container that has a block overload (<c>[ViewBuilder] Action</c>), when one of its arguments is a
/// conditional or <see langword="null"/>.
/// </remarks>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ConvertToBlockRefactoringProvider))]
[Shared]
public sealed class ConvertToBlockRefactoringProvider : CodeRefactoringProvider
{
    private const string Title = "Convert to block";

    /// <inheritdoc/>
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SemanticModel? model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
            return;

        // The innermost container around the selection.
        InvocationExpressionSyntax? container = root.FindNode(context.Span).AncestorsAndSelf().OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(invocation => IsContainer(invocation, model, context.CancellationToken));
        if (container is null || !container.ArgumentList.Arguments.Any(a => IsConditional(a.Expression)))
            return;

        context.RegisterRefactoring(CodeAction.Create(
            Title,
            cancellation => ConvertAsync(context.Document, container, model, cancellation),
            equivalenceKey: Title));
    }

    // A call that receives its children as separate arguments (params View[]), whose method has a block overload.
    private static bool IsContainer(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellation)
    {
        if (model.GetOperation(invocation, cancellation) is not IInvocationOperation { TargetMethod: { } method } call
            || method.Parameters.Length != 1
            || call.Arguments.Length != 1
            || call.Arguments[0].ArgumentKind != ArgumentKind.ParamArray
            || invocation.ArgumentList.Arguments.Any(a => a.NameColon is not null))
        {
            return false;
        }

        return method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>().Any(overload =>
            overload.Parameters.Length == 1
            && overload.Parameters[0].Type is INamedTypeSymbol { TypeArguments.Length: 0 }
            && ViewBuilderSyntax.IsBlockParameter(overload.Parameters[0]));
    }

    private static bool IsConditional(ExpressionSyntax expression) =>
        Unparenthesized(expression) is ConditionalExpressionSyntax || IsNothing(expression);

    // null or default: an argument that adds no view.
    private static bool IsNothing(ExpressionSyntax expression) =>
        Unparenthesized(expression).Kind() is SyntaxKind.NullLiteralExpression or SyntaxKind.DefaultLiteralExpression;

    private static ExpressionSyntax Unparenthesized(ExpressionSyntax expression) =>
        expression is ParenthesizedExpressionSyntax parenthesized ? Unparenthesized(parenthesized.Expression) : expression;

    private static async Task<Document> ConvertAsync(
        Document document, InvocationExpressionSyntax container, SemanticModel model, CancellationToken cancellation)
    {
        SyntaxNode root = (await document.GetSyntaxRootAsync(cancellation).ConfigureAwait(false))!;

        // Each argument becomes a statement, which keeps the comments written before it.
        List<StatementSyntax> statements = [];
        foreach (ArgumentSyntax argument in container.ArgumentList.Arguments)
        {
            if (StatementOf(argument.Expression, model, cancellation) is { } statement)
                statements.Add(statement.WithLeadingTrivia(argument.GetLeadingTrivia().Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia))));
        }

        // The line break after the arguments is kept, so that modifiers written on their own lines stay there.
        ParenthesizedLambdaExpressionSyntax block = SyntaxFactory.ParenthesizedLambdaExpression(
            SyntaxFactory.ParameterList(), SyntaxFactory.Block(statements));
        ArgumentListSyntax arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(block)))
            .WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTrailingTrivia(container.ArgumentList.CloseParenToken.TrailingTrivia));
        InvocationExpressionSyntax converted = container
            .WithArgumentList(arguments)
            .WithAdditionalAnnotations(Formatter.Annotation);
        return document.WithSyntaxRoot(root.ReplaceNode(container, converted));
    }

    // The statement that adds the view of an argument: nothing for null, an if for a conditional, the call itself for a
    // view that the generator intercepts, and Show for any other view.
    private static StatementSyntax? StatementOf(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellation)
    {
        if (IsNothing(expression))
            return null;

        ExpressionSyntax original = Unparenthesized(expression);
        if (original is ConditionalExpressionSyntax conditional)
        {
            StatementSyntax? whenTrue = StatementOf(conditional.WhenTrue, model, cancellation);
            StatementSyntax? whenFalse = StatementOf(conditional.WhenFalse, model, cancellation);
            if (whenTrue is null)
                return whenFalse is null ? null : SyntaxFactory.IfStatement(Negate(conditional.Condition), whenFalse);

            return whenFalse is null
                ? SyntaxFactory.IfStatement(conditional.Condition.WithoutTrivia(), whenTrue)
                : SyntaxFactory.IfStatement(conditional.Condition.WithoutTrivia(), whenTrue, SyntaxFactory.ElseClause(whenFalse));
        }

        bool intercepted = model.GetSymbolInfo(original, cancellation).Symbol is IMethodSymbol method
            && ViewBuilderSyntax.IsInterceptable(method);
        ExpressionSyntax view = original.WithoutTrivia();
        return SyntaxFactory.ExpressionStatement(intercepted
            ? view
            : SyntaxFactory.InvocationExpression(
                FactoryNames.Of(ViewBuilderSyntax.ShowName),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(view)))));
    }

    private static PrefixUnaryExpressionSyntax Negate(ExpressionSyntax condition) => SyntaxFactory.PrefixUnaryExpression(
        SyntaxKind.LogicalNotExpression,
        SyntaxFactory.ParenthesizedExpression(condition.WithoutTrivia()).WithAdditionalAnnotations(Simplifier.Annotation));
}
