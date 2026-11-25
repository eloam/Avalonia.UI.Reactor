// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Views;

/// <summary>
/// Marks a parameter that receives a view builder block: a lambda whose view statements become the children of the
/// container, as with <c>@ViewBuilder</c> in SwiftUI.
/// </summary>
/// <remarks>
/// In a block, a statement that creates a view (<c>Text("a");</c>, <c>Row(todo);</c>) adds it to the block, and
/// <see langword="if"/>, <see langword="else"/>, <see langword="switch"/> and local variables decide which views are
/// added. The generator of the package rewrites these statements at compile time with interceptors.
/// </remarks>
/// <example>
/// <code>
/// public static StackView Card([ViewBuilder] Action content) =&gt; VStack(content).Padding(16);
///
/// Card(() =&gt;
/// {
///     Text("Title").Bold();
///     if (HasDetails)
///         Text(Details);
/// })
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ViewBuilderAttribute : Attribute;
