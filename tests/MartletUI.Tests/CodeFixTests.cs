// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections.Immutable;
using MartletUI.CodeFixes;
using MartletUI.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using static MartletUI.Tests.Snippets;

namespace MartletUI.Tests;

/// <summary>
/// The code fixes and the refactoring of view builder blocks, applied to code snippets in a workspace; the fixed code
/// compiles without diagnostics.
/// </summary>
public class CodeFixTests
{
    private const string CounterComponent = """
        public class Counter : Component
        {
            public override View Body => Text("0");
        }

        """;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_component_alone_in_a_block_is_wrapped_in_Show()
    {
        string? code = await FixAsync(new ShowCodeFixProvider(), "CS0201", CounterComponent + """
            public class Page : Component
            {
                private readonly Counter _counter = new();

                public override View Body => VStack(() =>
                {
                    Text("Title");
                    _counter;
                });
            }
            """);

        Assert.Equal(CounterComponent + """
            public class Page : Component
            {
                private readonly Counter _counter = new();

                public override View Body => VStack(() =>
                {
                    Text("Title");
                    Show(_counter);
                });
            }
            """, code);
        Assert.Empty(Compile(code!));
    }

    [Fact]
    public async Task A_conditional_alone_in_a_block_is_wrapped_in_Show()
    {
        string? code = await FixAsync(new ShowCodeFixProvider(), "CS0201", """
            public class Page(bool compact) : Component
            {
                public override View Body => VStack(() =>
                {
                    !compact ? Text("Long") : null;
                });
            }
            """);

        Assert.Equal("""
            public class Page(bool compact) : Component
            {
                public override View Body => VStack(() =>
                {
                    Show(!compact ? Text("Long") : null);
                });
            }
            """, code);
        Assert.Empty(Compile(code!));
    }

    [Fact]
    public async Task A_loop_in_a_block_is_converted_to_ForEach()
    {
        string? code = await FixAsync(new ForEachCodeFixProvider(), "MVU014", """
            public class Page : Component
            {
                private readonly StateList<string> _names = ["Ada", "Grace"];

                public override View Body => VStack(() =>
                {
                    foreach (string name in _names)
                    {
                        Text(name);
                        if (name.Length > 3)
                            Text("Long");
                    }
                });
            }
            """);

        Assert.Equal("""
            public class Page : Component
            {
                private readonly StateList<string> _names = ["Ada", "Grace"];

                public override View Body => VStack(() =>
                {
                    ForEach(_names, name =>
                    {
                        Text(name);
                        if (name.Length > 3)
                            Text("Long");
                    });
                });
            }
            """, code);
        Assert.Empty(Compile(code!));
    }

    [Fact]
    public async Task A_loop_of_one_view_becomes_a_ForEach_of_one_view()
    {
        string? code = await FixAsync(new ForEachCodeFixProvider(), "MVU014", """
            public class Page : Component
            {
                private readonly StateList<string> _names = ["Ada", "Grace"];

                public override View Body => VStack(() =>
                {
                    foreach (var name in _names)
                        Text(name);
                });
            }
            """);

        Assert.Equal("""
            public class Page : Component
            {
                private readonly StateList<string> _names = ["Ada", "Grace"];

                public override View Body => VStack(() =>
                {
                    ForEach(_names, name => Text(name));
                });
            }
            """, code);
        Assert.Empty(Compile(code!));
    }

    [Fact]
    public async Task A_loop_that_breaks_is_not_converted() =>
        Assert.Null(await FixAsync(new ForEachCodeFixProvider(), "MVU014", """
            public class Page : Component
            {
                private readonly StateList<string> _names = ["Ada", "Grace"];

                public override View Body => VStack(() =>
                {
                    foreach (string name in _names)
                    {
                        if (name.Length == 0)
                            break;
                        Text(name);
                    }
                });
            }
            """));

    [Fact]
    public async Task Arguments_with_a_condition_are_converted_to_a_block()
    {
        string? code = await RefactorAsync(new ConvertToBlockRefactoringProvider(), "compact ?", CounterComponent + """
            public class Page(bool compact) : Component
            {
                private readonly Counter _counter = new();

                public override View Body =>
                    VStack(
                        Text("Title"),
                        // Only in the full layout.
                        compact ? null : Text("Subtitle"),
                        _counter)
                    .Spacing(8);
            }
            """);

        Assert.Equal(CounterComponent + """
            public class Page(bool compact) : Component
            {
                private readonly Counter _counter = new();

                public override View Body =>
                    VStack(() =>
                    {
                        Text("Title");
                        // Only in the full layout.
                        if (!compact)
                            Text("Subtitle");
                        Show(_counter);
                    })
                    .Spacing(8);
            }
            """, code);
        Assert.Empty(Compile(code!));
    }

    [Fact]
    public async Task Arguments_without_a_condition_are_not_converted() =>
        Assert.Null(await RefactorAsync(new ConvertToBlockRefactoringProvider(), "Title", """
            public class Page : Component
            {
                public override View Body => VStack(Text("Title"), Text("Subtitle"));
            }
            """));

    // Applies the fix offered for the first diagnostic with this identifier; returns the code after the usings, or null
    // if no fix is offered.
    private static async Task<string?> FixAsync(CodeFixProvider provider, string id, string code)
    {
        using var workspace = new AdhocWorkspace();
        Document document = AddDocument(workspace, code);
        Compilation compilation = (await document.Project.GetCompilationAsync(Cancellation))!;
        ImmutableArray<Diagnostic> diagnostics = await compilation
            .WithAnalyzers([new StateAnalyzer(), new ViewBuilderAnalyzer()])
            .GetAllDiagnosticsAsync(Cancellation);

        List<CodeAction> actions = [];
        Diagnostic diagnostic = diagnostics.First(d => d.Id == id);
        await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), Cancellation));
        return actions.Count == 0 ? null : await ApplyAsync(document, Assert.Single(actions));
    }

    // Applies the refactoring offered at the start of a piece of text; returns the code after the usings, or null if no
    // refactoring is offered.
    private static async Task<string?> RefactorAsync(CodeRefactoringProvider provider, string at, string code)
    {
        using var workspace = new AdhocWorkspace();
        Document document = AddDocument(workspace, code);
        List<CodeAction> actions = [];
        var span = new TextSpan(Usings.Length + code.IndexOf(at, StringComparison.Ordinal), 0);
        await provider.ComputeRefactoringsAsync(new CodeRefactoringContext(document, span, actions.Add, Cancellation));
        return actions.Count == 0 ? null : await ApplyAsync(document, Assert.Single(actions));
    }

    private static Document AddDocument(AdhocWorkspace workspace, string code) => workspace
        .AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(), VersionStamp.Create(), "Snippet", "Snippet", LanguageNames.CSharp,
            compilationOptions: Snippets.CompilationOptions, parseOptions: Snippets.ParseOptions, metadataReferences: References))
        .AddDocument("Snippet.cs", Usings + code);

    private static async Task<string> ApplyAsync(Document document, CodeAction action)
    {
        ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(Cancellation);
        Solution solution = Assert.Single(operations.OfType<ApplyChangesOperation>()).ChangedSolution;
        string text = (await solution.GetDocument(document.Id)!.GetTextAsync(Cancellation)).ToString();
        return text[Usings.Length..];
    }
}
