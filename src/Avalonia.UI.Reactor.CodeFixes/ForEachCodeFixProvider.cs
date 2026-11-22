// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.UI.Reactor.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Avalonia.UI.Reactor.CodeFixes;

/// <summary>
/// Provides the code fix "Convert to 'ForEach'" for a <see langword="foreach"/> loop in a view builder block (MVU014).
/// </summary>
/// <remarks>
/// <c>foreach (var todo in Todos) { Row(todo); }</c> becomes <c>ForEach(Todos, todo =&gt; { Row(todo); });</c>: each item
/// gets its own row, re-evaluated on its own and keyed by the item. The fix is not offered when the body leaves the loop
/// (<see langword="break"/>, <see langword="continue"/>, <see langword="return"/>, <see langword="goto"/>), whose
/// meaning would change in a lambda, or when the loop converts its items to another type.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ForEachCodeFixProvider))]
[Shared]
public sealed class ForEachCodeFixProvider : CodeFixProvider
{
    private const string Title = "Convert to 'ForEach'";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [ViewBuilderAnalyzer.InvalidStatement.Id];

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
            SyntaxToken token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (token.Parent is not ForEachStatementSyntax loop || token != loop.ForEachKeyword || !CanConvert(loop, model))
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(Title, cancellation => ConvertAsync(context.Document, loop, cancellation), equivalenceKey: Title),
                diagnostic);
        }
    }

    private static bool CanConvert(ForEachStatementSyntax loop, SemanticModel model)
    {
        if (loop.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
            return false;

        // The items are used as they are: 'var', or their own type.
        ForEachStatementInfo info = model.GetForEachStatementInfo(loop);
        if (!loop.Type.IsVar && !info.ElementConversion.IsIdentity)
            return false;

        // A jump out of the body would leave the row, not the loop. Jumps of a nested function stay in it.
        return !loop.Statement.DescendantNodesAndSelf(node => node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
            .Any(node => node is BreakStatementSyntax or ContinueStatementSyntax or ReturnStatementSyntax or GotoStatementSyntax);
    }

    private static async Task<Document> ConvertAsync(Document document, ForEachStatementSyntax loop, CancellationToken cancellation)
    {
        SyntaxNode root = (await document.GetSyntaxRootAsync(cancellation).ConfigureAwait(false))!;

        // A single view call stays a single expression: ForEach(Todos, todo => Row(todo)).
        CSharpSyntaxNode body = loop.Statement switch
        {
            BlockSyntax block => block,
            ExpressionStatementSyntax statement => statement.Expression,
            StatementSyntax statement => SyntaxFactory.Block(statement),
        };
        SimpleLambdaExpressionSyntax row = SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(loop.Identifier.WithoutTrivia()), body.WithoutTrivia());
        ExpressionStatementSyntax forEach = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                FactoryNames.Of(FactoryNames.ForEach),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([SyntaxFactory.Argument(loop.Expression.WithoutTrivia()), SyntaxFactory.Argument(row)]))))
            .WithTriviaFrom(loop)
            .WithAdditionalAnnotations(Formatter.Annotation);
        return document.WithSyntaxRoot(root.ReplaceNode(loop, forEach));
    }
}
