// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents a single line of text; a line too long for its place ends with an ellipsis.
/// </summary>
/// <param name="Content">The text to display.</param>
public sealed record TextView(string Content) : View
{
    /// <summary>
    /// Gets the horizontal alignment of the text in its place.
    /// </summary>
    public TextAlign TextAlign { get; init; }
}
