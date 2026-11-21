// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents a vertical or horizontal stack of views.
/// </summary>
/// <param name="Axis">The direction of the stack.</param>
/// <param name="Children">The views of the stack.</param>
public sealed record StackView(Axis Axis, IReadOnlyList<View> Children) : View
{
    /// <summary>
    /// Gets the space between the children, or <see langword="null"/> for the spacing of the theme.
    /// </summary>
    public float? Spacing { get; init; }

    /// <summary>
    /// Gets the alignment of the children on the cross axis.
    /// </summary>
    public Align CrossAlign { get; init; }
}
