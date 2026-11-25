// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MartletUI.Generators;

/// <summary>
/// Reports the statements of view builder blocks that do not describe views.
/// </summary>
/// <remarks>
/// A block (<c>VStack(() =&gt; { … })</c>) runs on every update of its view: each statement adds a view, or decides
/// which views are added. The analyzer reports the other statements, a loop for example (MVU014), and a
/// <see langword="switch"/> on an enumeration that misses some of its values, which would show nothing (MVU015). A change
/// of state is reported by <see cref="StateAnalyzer"/> (MVU018).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ViewBuilderAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Usage";
    private const string InvalidStatementId = "MVU014";
    private const string InvalidStatementTitle = "Statement in a view block does not produce a view";
    private const string InvalidStatementDescription =
        "A view builder block ('VStack(() => { … })') describes the views of its container and runs on every update: each "
        + "statement adds a view ('Text(…);', 'Show(…);') or decides which views are added ('if', 'switch', local "
        + "variables, 'return'). Move any other statement out of the block, into an action for example, show the items of "
        + "a list with 'ForEach', and add any other view with 'Show(…)'.";

    // The second part of the message of MVU014: what to write instead.
    private const string MoveOutOfBlock = "move it out of the block, for example into an action";
    private const string WrapInShow = "wrap the view in 'Show(…)'";
    private const string UseForEach = "show a view per item with 'ForEach(items, item => { … })'";

    private const string ConditionalAttributeName = "System.Diagnostics.ConditionalAttribute";
    private const string FlagsAttributeName = "System.FlagsAttribute";

    // The separators of the words of a statement quoted in a message, which is written on one line.
    private static readonly char[] s_whitespace = [' ', '\t', '\r', '\n'];

    /// <summary>
    /// Describes MVU014: a statement of a block that adds no view, a loop for example.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidStatement = new(
        id: InvalidStatementId,
        title: InvalidStatementTitle,
        messageFormat: "'{0}' does not add a view to the block; {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: InvalidStatementDescription);

    /// <summary>
    /// Describes MVU014 for an <see langword="async"/> block, whose views after an <see langword="await"/> are lost.
    /// </summary>
    public static readonly DiagnosticDescriptor AsyncBlock = new(
        id: InvalidStatementId,
        title: InvalidStatementTitle,
        messageFormat: "A view builder block cannot be 'async', because the views it adds after an 'await' are lost; remove 'async' and start the work with '.Task(…)'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: InvalidStatementDescription);

    /// <summary>
    /// Describes MVU015: a <see langword="switch"/> statement of a block that misses values of an enumeration.
    /// </summary>
    public static readonly DiagnosticDescriptor IncompleteSwitch = new(
        id: "MVU015",
        title: "Switch in a view block does not handle every value",
        messageFormat: "The switch on '{0}' does not handle {1}, so these values show nothing; add a 'case' for each of them, or a 'default' section",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Unlike a 'switch' expression, a 'switch' statement does not check that every value of an enumeration is handled: a value added later shows nothing, without an error. Add a 'case' for every value, or a 'default' section. A 'switch' expression passed to 'Show' is checked by the compiler.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [InvalidStatement, AsyncBlock, IncompleteSwitch];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeLambda,
            SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.AnonymousMethodExpression);
    }

    private static void AnalyzeLambda(SyntaxNodeAnalysisContext c)
    {
        var lambda = (AnonymousFunctionExpressionSyntax)c.Node;
        if (!ViewBuilderSyntax.IsBlockLambda(lambda, c.SemanticModel, c.CancellationToken))
            return;

        if (lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
            c.ReportDiagnostic(Diagnostic.Create(AsyncBlock, lambda.AsyncKeyword.GetLocation()));

        if (lambda.Block is { } block)
            CheckStatement(c, block);
        else if (lambda.ExpressionBody is { } expression)
            CheckExpression(c, expression);
    }

    // Blocks, if and switch are followed: their statements are statements of the block.
    private static void CheckStatement(SyntaxNodeAnalysisContext c, StatementSyntax statement)
    {
        switch (statement)
        {
            case BlockSyntax block:
                foreach (StatementSyntax inner in block.Statements)
                    CheckStatement(c, inner);
                break;
            case IfStatementSyntax ifStatement:
                CheckStatement(c, ifStatement.Statement);
                if (ifStatement.Else is { } elseClause)
                    CheckStatement(c, elseClause.Statement);
                break;
            case SwitchStatementSyntax switchStatement:
                CheckSwitch(c, switchStatement);
                foreach (StatementSyntax inner in switchStatement.Sections.SelectMany(s => s.Statements))
                    CheckStatement(c, inner);
                break;
            case LocalDeclarationStatementSyntax local:
                if (local.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                    Report(c, local.UsingKeyword, MoveOutOfBlock);
                break;
            case ReturnStatementSyntax or BreakStatementSyntax or ThrowStatementSyntax or EmptyStatementSyntax:
                break;
            case ExpressionStatementSyntax expressionStatement:
                CheckExpression(c, expressionStatement.Expression);
                break;
            case CommonForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax or DoStatementSyntax:
                Report(c, statement.GetFirstToken(), UseForEach);
                break;
            case LocalFunctionStatementSyntax localFunction:
                Report(c, localFunction.Identifier, MoveOutOfBlock);
                break;
            default:
                Report(c, statement.GetFirstToken(), MoveOutOfBlock);
                break;
        }
    }

    // A view statement is intercepted when it can be; Show adds any view; a [Conditional] call (Debug.WriteLine) and a
    // discard are allowed. A statement that does not compile is left to the compiler.
    private static void CheckExpression(SyntaxNodeAnalysisContext c, ExpressionSyntax expression)
    {
        string fix;
        string name;
        IOperation? operation = c.SemanticModel.GetOperation(expression, c.CancellationToken);
        if (operation is not null && c.ContainingSymbol is { } member)
        {
            // MVU018 says why state cannot change here, and where to change it.
            (ImmutableArray<string> names, bool refresh) = StateChange.InView(operation, member, c.CancellationToken);
            if (refresh || !names.IsEmpty)
                return;
        }

        switch (operation)
        {
            case null or IInvalidOperation:
            case ISimpleAssignmentOperation { Target: IDiscardOperation }:
                return;
            case IInvocationOperation { TargetMethod: { } method }:
                if (ViewBuilderSyntax.IsShow(method)
                    || (ViewBuilderSyntax.IsView(method.ReturnType) && ViewBuilderSyntax.IsInterceptable(method))
                    || (method.ReturnsVoid && method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == ConditionalAttributeName)))
                {
                    return;
                }

                name = method.Name;
                fix = ViewBuilderSyntax.IsViewOrComponent(method.ReturnType) ? WrapInShow : MoveOutOfBlock;
                break;
            case IObjectCreationOperation { Type: { } type }:
                name = "new " + type.Name;
                fix = ViewBuilderSyntax.IsViewOrComponent(type) ? WrapInShow : MoveOutOfBlock;
                break;
            default:
                name = string.Join(" ", expression.ToString().Split(s_whitespace, StringSplitOptions.RemoveEmptyEntries));
                fix = MoveOutOfBlock;
                break;
        }

        c.ReportDiagnostic(Diagnostic.Create(InvalidStatement, expression.GetLocation(), name, fix));
    }

    private static void Report(SyntaxNodeAnalysisContext c, SyntaxToken token, string fix) =>
        c.ReportDiagnostic(Diagnostic.Create(InvalidStatement, token.GetLocation(), token.ValueText, fix));

    // MVU015: the values of an enumeration that no case handles, when there is no default. A pattern that the analyzer
    // does not read (not, relational, a guard) is assumed to handle every value, so that it never warns wrongly.
    private static void CheckSwitch(SyntaxNodeAnalysisContext c, SwitchStatementSyntax syntax)
    {
        if (c.SemanticModel.GetOperation(syntax, c.CancellationToken) is not ISwitchOperation switchOperation)
            return;
        ITypeSymbol? type = switchOperation.Value.Type;
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            type = nullable.TypeArguments[0];
        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumeration
            || enumeration.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == FlagsAttributeName))
        {
            return;
        }

        var handled = new HashSet<object?>();
        foreach (ICaseClauseOperation clause in switchOperation.Cases.SelectMany(s => s.Clauses))
        {
            bool handlesEverything = clause switch
            {
                IDefaultCaseClauseOperation => true,
                ISingleValueCaseClauseOperation single => !Add(handled, single.Value),
                IPatternCaseClauseOperation { Guard: not null } => false,
                IPatternCaseClauseOperation pattern => HandlesEverything(pattern.Pattern, handled),
                _ => true,
            };
            if (handlesEverything)
                return;
        }

        // One name per missing value: an alias of a handled value is handled too.
        List<string> missing = [];
        foreach (IFieldSymbol value in enumeration.GetMembers().OfType<IFieldSymbol>().Where(f => f.HasConstantValue))
        {
            if (handled.Add(value.ConstantValue))
                missing.Add($"'{enumeration.Name}.{value.Name}'");
        }

        if (missing.Count == 0)
            return;

        string values = missing.Count == 1
            ? missing[0]
            : string.Join(", ", missing.Take(missing.Count - 1)) + " and " + missing[missing.Count - 1];
        c.ReportDiagnostic(Diagnostic.Create(IncompleteSwitch, syntax.SwitchKeyword.GetLocation(), syntax.Expression.ToString(), values));
    }

    // Adds the constant values that a pattern matches; returns true if it matches any value. Both sides of an 'or' are
    // read (|, not ||) so that all their values are added.
    private static bool HandlesEverything(IPatternOperation pattern, HashSet<object?> handled) => pattern switch
    {
        IConstantPatternOperation constant => !Add(handled, constant.Value),
        IBinaryPatternOperation { OperatorKind: BinaryOperatorKind.Or } or =>
            HandlesEverything(or.LeftPattern, handled) | HandlesEverything(or.RightPattern, handled),
        _ => true,
    };

    // Adds a constant value; returns false if the value is not a constant.
    private static bool Add(HashSet<object?> handled, IOperation value)
    {
        if (!value.ConstantValue.HasValue)
            return false;
        handled.Add(value.ConstantValue.Value);
        return true;
    }
}
