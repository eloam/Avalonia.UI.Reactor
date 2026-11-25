// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Generators;
using MartletUI.State;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MartletUI.Tests;

/// <summary>
/// Compiles code snippets in memory with the generators and the analyzers of the framework, as an application would.
/// </summary>
internal static class Snippets
{
    /// <summary>
    /// The usings written before every snippet.
    /// </summary>
    public const string Usings =
        "using System; using System.Collections.Generic; using MartletUI.Primitives; using MartletUI.State; " +
        "using MartletUI.Theming; using MartletUI.Views; using static MartletUI.Views.Ui;\n";

    /// <summary>
    /// Gets the parse options of the snippets: the latest language, and the interceptors of view builder blocks allowed,
    /// as the build rules of the package allow them.
    /// </summary>
    public static CSharpParseOptions ParseOptions { get; } = new CSharpParseOptions(LanguageVersion.Preview)
        .WithFeatures([new("InterceptorsNamespaces", "MartletUI.Generated")]);

    /// <summary>
    /// Gets the compilation options of the snippets: a library with nullable reference types.
    /// </summary>
    public static CSharpCompilationOptions CompilationOptions { get; } =
        new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);

    /// <summary>
    /// Gets the references of the snippets: the runtime and the core of the framework.
    /// </summary>
    public static IReadOnlyList<MetadataReference> References { get; } =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(Component).Assembly.Location)
            .DistinctBy(Path.GetFileName)
            .Select(p => MetadataReference.CreateFromFile(p)),
    ];

    /// <summary>
    /// Compiles a snippet and returns the distinct identifiers of its warnings and errors.
    /// </summary>
    /// <param name="code">The snippet, written after <see cref="Usings"/>.</param>
    /// <returns>The identifiers of the diagnostics.</returns>
    public static string[] Ids(string code) => [.. Compile(code).Select(d => d.Id).Distinct()];

    /// <summary>
    /// Compiles a snippet with the generators and the analyzers; returns the warnings and errors.
    /// </summary>
    /// <param name="code">The snippet, written after <see cref="Usings"/>.</param>
    /// <returns>The warnings and errors, of the compiler and of the analyzers.</returns>
    public static IReadOnlyList<Diagnostic> Compile(string code)
    {
        CSharpCompilation compilation = CreateCompilation(code);
        CSharpGeneratorDriver.Create(
                [new StateGenerator().AsSourceGenerator(), new ViewBuilderGenerator().AsSourceGenerator()],
                parseOptions: ParseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out Compilation? output, out _);

        return
        [
            .. output.WithAnalyzers([new StateAnalyzer(), new ViewBuilderAnalyzer()])
                .GetAllDiagnosticsAsync().Result
                .Where(d => d.Severity >= DiagnosticSeverity.Warning),
        ];
    }

    /// <summary>
    /// Runs the generator of the <c>[State]</c> properties on a snippet.
    /// </summary>
    /// <param name="code">The snippet, written after <see cref="Usings"/>.</param>
    /// <returns>The text of each generated file, by name.</returns>
    public static IReadOnlyDictionary<string, string> Generate(string code) =>
        CSharpGeneratorDriver.Create([new StateGenerator().AsSourceGenerator()], parseOptions: ParseOptions)
            .RunGenerators(CreateCompilation(code))
            .GetRunResult()
            .Results.Single()
            .GeneratedSources.ToDictionary(source => source.HintName, source => source.SourceText.ToString());

    private static CSharpCompilation CreateCompilation(string code) =>
        CSharpCompilation.Create("Snippet", [CSharpSyntaxTree.ParseText(Usings + code, ParseOptions)], References, CompilationOptions);
}
