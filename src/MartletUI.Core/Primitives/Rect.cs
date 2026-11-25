// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Primitives;

/// <summary>
/// Represents a rectangle, in device-independent units.
/// </summary>
/// <param name="X">The left edge.</param>
/// <param name="Y">The top edge.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    /// <summary>
    /// Gets the right edge.
    /// </summary>
    public float Right => X + Width;

    /// <summary>
    /// Gets the bottom edge.
    /// </summary>
    public float Bottom => Y + Height;

    /// <summary>
    /// Gets the center.
    /// </summary>
    public Point Center => new(X + Width / 2, Y + Height / 2);

    /// <summary>
    /// Returns the largest square centered in this rectangle, for circles and gauges.
    /// </summary>
    /// <param name="inset">The margin to keep inside the rectangle, on each side.</param>
    /// <returns>The centered square.</returns>
    public Rect CenteredSquare(float inset = 0)
    {
        float side = Math.Max(0, Math.Min(Width, Height) - 2 * inset);
        return new(X + (Width - side) / 2, Y + (Height - side) / 2, side, side);
    }
}
