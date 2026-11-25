// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;

namespace MartletUI.Theming;

/// <summary>
/// Represents a color that is either fixed, a role of the theme, or a color of the theme designated by its name.
/// </summary>
/// <example>
/// <code>
/// .Background("#FF0000")          // fixed
/// .Background(ColorRole.Surface)  // role: follows the current theme
/// .Background("success")          // named color added to the theme by WithColor("success", …)
/// </code>
/// </example>
public readonly record struct ColorValue
{
    private readonly Color _color;
    private readonly ColorRole? _role;
    private readonly string? _name;

    private ColorValue(Color color, ColorRole? role, string? name)
    {
        _color = color;
        _role = role;
        _name = name;
    }

    /// <summary>
    /// Converts a fixed color.
    /// </summary>
    /// <param name="color">The color.</param>
    public static implicit operator ColorValue(Color color) => new(color, null, null);

    /// <summary>
    /// Converts a color role, resolved by the current theme.
    /// </summary>
    /// <param name="role">The role.</param>
    public static implicit operator ColorValue(ColorRole role) => new(default, role, null);

    /// <summary>
    /// Converts a text: <c>"#RRGGBB"</c> or <c>"#RRGGBBAA"</c> is a fixed color; any other text is the name of a color
    /// of the theme.
    /// </summary>
    /// <param name="value">The color or the name of a theme color.</param>
    public static implicit operator ColorValue(string value) =>
        value.StartsWith('#') ? new(Color.Hex(value), null, null) : new(default, null, value);

    /// <summary>
    /// Resolves this value against a palette.
    /// </summary>
    /// <param name="palette">The palette of the current theme.</param>
    /// <returns>The color of the role or name in the palette, or the fixed color.</returns>
    public Color Resolve(Palette palette) =>
        _role is { } role ? palette[role] : _name is { } name ? palette[name] : _color;
}
