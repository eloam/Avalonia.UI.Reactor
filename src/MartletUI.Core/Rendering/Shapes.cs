// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;

namespace MartletUI.Rendering;

/// <summary>
/// Provides the shapes of the standard indicators, in a unit square (from 0 to 1), scaled to their box.
/// </summary>
/// <remarks>
/// Another shape goes through a control template.
/// </remarks>
internal static class Shapes
{
    /// <summary>
    /// The box size for which <see cref="Theming.CheckboxTheme.CheckThickness"/> is given; the line scales with the box.
    /// </summary>
    public const float CheckboxReferenceSize = 18;

    /// <summary>
    /// The check mark of a checkbox, designed on an 18-unit box.
    /// </summary>
    public static readonly Point[] CheckboxMark =
    [
        new(4.5f / CheckboxReferenceSize, 9.5f / CheckboxReferenceSize),
        new(7.8f / CheckboxReferenceSize, 12.8f / CheckboxReferenceSize),
        new(13.5f / CheckboxReferenceSize, 5.8f / CheckboxReferenceSize),
    ];

    /// <summary>
    /// Scales a shape to a box.
    /// </summary>
    /// <param name="shape">The points of the shape, in a unit square.</param>
    /// <param name="box">The box to fit the shape in.</param>
    /// <returns>The points of the shape in the box.</returns>
    public static Point[] Fit(Point[] shape, Rect box) =>
        [.. shape.Select(p => new Point(box.X + p.X * box.Width, box.Y + p.Y * box.Height))];
}
