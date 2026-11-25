// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Primitives;

/// <summary>
/// Represents the thickness of a frame around a rectangle, such as a padding.
/// </summary>
/// <param name="Left">The left thickness.</param>
/// <param name="Top">The top thickness.</param>
/// <param name="Right">The right thickness.</param>
/// <param name="Bottom">The bottom thickness.</param>
public readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Thickness"/> structure with the same thickness on every side.
    /// </summary>
    /// <param name="all">The thickness of every side.</param>
    public Thickness(float all)
        : this(all, all, all, all)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Thickness"/> structure with a horizontal and a vertical thickness.
    /// </summary>
    /// <param name="horizontal">The thickness of the left and right sides.</param>
    /// <param name="vertical">The thickness of the top and bottom sides.</param>
    public Thickness(float horizontal, float vertical)
        : this(horizontal, vertical, horizontal, vertical)
    {
    }

    /// <summary>
    /// Gets the sum of the left and right thicknesses.
    /// </summary>
    public float Horizontal => Left + Right;

    /// <summary>
    /// Gets the sum of the top and bottom thicknesses.
    /// </summary>
    public float Vertical => Top + Bottom;
}
