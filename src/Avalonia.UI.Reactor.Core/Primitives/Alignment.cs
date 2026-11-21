// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Primitives;

/// <summary>
/// Represents a position inside a rectangle, on both axes, used by <c>ZStack</c> and <c>Overlay</c>.
/// </summary>
/// <param name="Horizontal">The alignment on the horizontal axis.</param>
/// <param name="Vertical">The alignment on the vertical axis.</param>
public readonly record struct Alignment(Align Horizontal, Align Vertical)
{
    /// <summary>
    /// Gets the top-left corner.
    /// </summary>
    public static readonly Alignment TopLeft = new(Align.Start, Align.Start);

    /// <summary>
    /// Gets the middle of the top edge.
    /// </summary>
    public static readonly Alignment Top = new(Align.Center, Align.Start);

    /// <summary>
    /// Gets the top-right corner.
    /// </summary>
    public static readonly Alignment TopRight = new(Align.End, Align.Start);

    /// <summary>
    /// Gets the middle of the left edge.
    /// </summary>
    public static readonly Alignment Left = new(Align.Start, Align.Center);

    /// <summary>
    /// Gets the center.
    /// </summary>
    public static readonly Alignment Center = new(Align.Center, Align.Center);

    /// <summary>
    /// Gets the middle of the right edge.
    /// </summary>
    public static readonly Alignment Right = new(Align.End, Align.Center);

    /// <summary>
    /// Gets the bottom-left corner.
    /// </summary>
    public static readonly Alignment BottomLeft = new(Align.Start, Align.End);

    /// <summary>
    /// Gets the middle of the bottom edge.
    /// </summary>
    public static readonly Alignment Bottom = new(Align.Center, Align.End);

    /// <summary>
    /// Gets the bottom-right corner.
    /// </summary>
    public static readonly Alignment BottomRight = new(Align.End, Align.End);

    /// <summary>
    /// Gets the alignment that fills the whole rectangle.
    /// </summary>
    public static readonly Alignment Fill = new(Align.Stretch, Align.Stretch);
}
