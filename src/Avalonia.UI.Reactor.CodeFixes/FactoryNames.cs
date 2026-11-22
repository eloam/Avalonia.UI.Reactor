// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Avalonia.UI.Reactor.CodeFixes;

/// <summary>
/// Provides the names of the view factories that the code fixes write.
/// </summary>
internal static class FactoryNames
{
    /// <summary>
    /// The name of the factory of lists.
    /// </summary>
    public const string ForEach = "ForEach";

    /// <summary>
    /// Creates the name of a factory of <c>Ui</c>, fully qualified and marked for simplification: it becomes
    /// <c>Show</c> in a file that imports <c>using static Avalonia.UI.Reactor.Views.Ui</c>, and <c>Ui.Show</c> in a
    /// file that imports its namespace only.
    /// </summary>
    /// <param name="factory">The name of the factory, for example <c>Show</c>.</param>
    /// <returns>The expression that names the factory.</returns>
    public static ExpressionSyntax Of(string factory) =>
        SyntaxFactory.ParseExpression($"global::{ViewBuilderSyntax.FactoriesTypeName}.{factory}")
            .WithAdditionalAnnotations(Simplifier.Annotation);
}
