// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MartletUI.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MartletUI.CodeFixes;

/// <summary>
/// Provides the code fix "Wrap in 'Show'" for a view written alone as a statement of a view builder block.
/// </summary>
/// <remarks>
/// C# accepts only calls, assignments and <see langword="new"/> as statements: a component (<c>_counter;</c>), a
/// conditional or a <see langword="switch"/> expression written alone in a block does not compile (CS0201).
/// <c>Show</c> adds it to the block.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ShowCodeFixProvider))]
[Shared]
public sealed class ShowCodeFixProvider : CodeFixProvider
{
    // CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement.
    private const string NotAStatementId = "CS0201";
    private const string Title = "Wrap in 'Show'";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [NotAStatementId];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SemanticModel? model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
            return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ExpressionStatementSyntax>() is not { } statement
                || ViewBuilderSyntax.BlockLambdaOf(statement) is not { } lambda
                || !ViewBuilderSyntax.MayBeBlockLambda(lambda, model, context.CancellationToken)
                || !IsView(statement.Expression, model, context.CancellationToken))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(Title, cancellation => WrapAsync(context.Document, statement, cancellation), equivalenceKey: Title),
                diagnostic);
        }
    }

    // A view or a component, or a conditional or a switch expression whose branches are views or null.
    private static bool IsView(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellation) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => IsView(parenthesized.Expression, model, cancellation),
        ConditionalExpressionSyntax conditional =>
            IsViewOrNull(conditional.WhenTrue, model, cancellation) && IsViewOrNull(conditional.WhenFalse, model, cancellation),
        SwitchExpressionSyntax switchExpression =>
            switchExpression.Arms.All(arm => IsViewOrNull(arm.Expression, model, cancellation)),
        _ => ViewBuilderSyntax.IsViewOrComponent(model.GetTypeInfo(expression, cancellation).Type),
    };

    private static bool IsViewOrNull(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellation) =>
        expression.IsKind(SyntaxKind.NullLiteralExpression) || IsView(expression, model, cancellation);

    private static async Task<Document> WrapAsync(Document document, ExpressionStatementSyntax statement, CancellationToken cancellation)
    {
        SyntaxNode root = (await document.GetSyntaxRootAsync(cancellation).ConfigureAwait(false))!;
        InvocationExpressionSyntax show = SyntaxFactory.InvocationExpression(
                FactoryNames.Of(ViewBuilderSyntax.ShowName),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(statement.Expression.WithoutTrivia()))))
            .WithTriviaFrom(statement.Expression);
        return document.WithSyntaxRoot(root.ReplaceNode(statement, statement.WithExpression(show)));
    }
}
