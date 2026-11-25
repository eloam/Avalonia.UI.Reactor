// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Primitives;

/// <summary>
/// Represents a point, in device-independent units.
/// </summary>
/// <param name="X">The horizontal coordinate.</param>
/// <param name="Y">The vertical coordinate.</param>
public readonly record struct Point(float X, float Y)
{
    /// <summary>
    /// Multiplies both coordinates of a point by a factor.
    /// </summary>
    /// <param name="point">The point to scale.</param>
    /// <param name="factor">The scale factor.</param>
    /// <returns>The scaled point.</returns>
    public static Point operator *(Point point, float factor) => new(point.X * factor, point.Y * factor);

    /// <summary>
    /// Adds the coordinates of two points.
    /// </summary>
    /// <param name="left">The first point.</param>
    /// <param name="right">The second point.</param>
    /// <returns>The sum of the two points.</returns>
    public static Point operator +(Point left, Point right) => new(left.X + right.X, left.Y + right.Y);

    /// <summary>
    /// Subtracts the coordinates of a point from another.
    /// </summary>
    /// <param name="left">The point to subtract from.</param>
    /// <param name="right">The point to subtract.</param>
    /// <returns>The difference of the two points.</returns>
    public static Point operator -(Point left, Point right) => new(left.X - right.X, left.Y - right.Y);
}
