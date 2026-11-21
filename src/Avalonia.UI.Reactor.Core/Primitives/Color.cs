// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Globalization;

namespace Avalonia.UI.Reactor.Primitives;

/// <summary>
/// Represents a color as red, green, blue and alpha components.
/// </summary>
/// <param name="R">The red component.</param>
/// <param name="G">The green component.</param>
/// <param name="B">The blue component.</param>
/// <param name="A">The alpha component: 0 is transparent, 255 is opaque.</param>
public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    /// <summary>
    /// Gets a fully transparent color.
    /// </summary>
    public static readonly Color Transparent = new(0, 0, 0, 0);

    /// <summary>
    /// Gets opaque white.
    /// </summary>
    public static readonly Color White = new(255, 255, 255);

    /// <summary>
    /// Gets opaque black.
    /// </summary>
    public static readonly Color Black = new(0, 0, 0);

    /// <summary>
    /// Parses a color written as in CSS.
    /// </summary>
    /// <param name="hex">The color, as <c>"#RRGGBB"</c> or <c>"#RRGGBBAA"</c>; the <c>#</c> is optional.</param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="FormatException"><paramref name="hex"/> is not a valid color.</exception>
    public static Color Hex(string hex)
    {
        string digits = hex.TrimStart('#');
        uint value = uint.Parse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return digits.Length switch
        {
            6 => new((byte)(value >> 16), (byte)(value >> 8), (byte)value),
            8 => new((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value),
            _ => throw new FormatException($"Invalid color '{hex}'. Use '#RRGGBB' or '#RRGGBBAA'."),
        };
    }

    /// <summary>
    /// Converts a color written as in CSS, so that views can be written <c>.Background("#2563EB")</c>.
    /// </summary>
    /// <param name="hex">The color, as <c>"#RRGGBB"</c> or <c>"#RRGGBBAA"</c>.</param>
    public static implicit operator Color(string hex) => Hex(hex);

    /// <summary>
    /// Returns this color with another opacity.
    /// </summary>
    /// <param name="opacity">The opacity, from 0 (transparent) to 1 (opaque).</param>
    /// <returns>The color with the new alpha component.</returns>
    public Color WithAlpha(float opacity) => this with { A = (byte)Math.Clamp(opacity * 255f, 0f, 255f) };

    /// <summary>
    /// Blends this color with another one.
    /// </summary>
    /// <param name="other">The color to blend with.</param>
    /// <param name="amount">The share of <paramref name="other"/>, from 0 (this color) to 1 (the other color).</param>
    /// <returns>The blended color.</returns>
    public Color Mix(Color other, float amount) =>
        new(Lerp(R, other.R, amount), Lerp(G, other.G, amount), Lerp(B, other.B, amount), Lerp(A, other.A, amount));

    private static byte Lerp(byte from, byte to, float amount) => (byte)Math.Round(from + (to - from) * amount);
}
