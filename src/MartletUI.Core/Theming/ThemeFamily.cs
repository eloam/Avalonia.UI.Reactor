// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Theming;

/// <summary>
/// Represents a family of themes: a light variant and/or a dark variant under one name.
/// </summary>
/// <remarks>
/// With both variants, the family follows the mode of the system, or the one set by <c>.ColorScheme(…)</c>; with a
/// single variant, that variant is always used.
/// </remarks>
public sealed record ThemeFamily
{
    /// <summary>
    /// Gets the Light and Dark themes provided by the framework.
    /// </summary>
    public static readonly ThemeFamily Standard = new("Standard", Theme.Light, Theme.Dark);

    // The variant that exists, used when the other one is missing: a family always has at least one.
    private readonly Theme _fallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeFamily"/> class.
    /// </summary>
    /// <param name="name">The name of the family.</param>
    /// <param name="light">The light variant, or <see langword="null"/>.</param>
    /// <param name="dark">The dark variant, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException">
    /// Both variants are <see langword="null"/>, or a variant does not have the expected color scheme.
    /// </exception>
    public ThemeFamily(string name, Theme? light, Theme? dark)
    {
        _fallback = light ?? dark
            ?? throw new ArgumentException($"Theme family '{name}' must contain at least one theme.");
        if (light is { Scheme: not ColorScheme.Light })
        {
            throw new ArgumentException(
                $"Theme '{light.Name}' is a dark theme and cannot be the light variant of theme family '{name}'.");
        }

        if (dark is { Scheme: not ColorScheme.Dark })
        {
            throw new ArgumentException(
                $"Theme '{dark.Name}' is a light theme and cannot be the dark variant of theme family '{name}'.");
        }

        Name = name;
        Light = light;
        Dark = dark;
    }

    /// <summary>
    /// Gets the name of the family.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the light variant, or <see langword="null"/> if the family has none.
    /// </summary>
    public Theme? Light { get; }

    /// <summary>
    /// Gets the dark variant, or <see langword="null"/> if the family has none.
    /// </summary>
    public Theme? Dark { get; }

    /// <summary>
    /// Gets a value indicating whether the family has a light and a dark variant, and therefore follows the system mode.
    /// </summary>
    public bool FollowsSystem => Light is not null && Dark is not null;

    /// <summary>
    /// Gets the themes of the family: one or two.
    /// </summary>
    public IEnumerable<Theme> Variants => new[] { Light, Dark }.OfType<Theme>();

    /// <summary>
    /// Creates a family from one or two themes, placed according to their color scheme in any order.
    /// </summary>
    /// <param name="name">The name of the family.</param>
    /// <param name="theme">A theme of the family.</param>
    /// <param name="other">The other theme of the family, or <see langword="null"/>.</param>
    /// <returns>The family.</returns>
    /// <exception cref="ArgumentException">Both themes have the same color scheme.</exception>
    public static ThemeFamily Of(string name, Theme theme, Theme? other = null)
    {
        if (other is not null && other.Scheme == theme.Scheme)
        {
            string scheme = theme.Scheme == ColorScheme.Dark ? "dark" : "light";
            throw new ArgumentException(
                $"Theme family '{name}' cannot contain two {scheme} themes; it takes one light theme and one dark theme.");
        }

        return new ThemeFamily(
            name,
            light: theme.Scheme == ColorScheme.Light ? theme : other,
            dark: theme.Scheme == ColorScheme.Dark ? theme : other);
    }

    /// <summary>
    /// Returns the variant for a color scheme or, if the family does not have it, the variant that exists.
    /// </summary>
    /// <param name="scheme">The color scheme.</param>
    /// <returns>The theme to use.</returns>
    public Theme For(ColorScheme scheme) => (scheme == ColorScheme.Dark ? Dark : Light) ?? _fallback;

    /// <summary>
    /// Determines whether a theme is one of the variants of the family.
    /// </summary>
    /// <param name="theme">The theme to look for.</param>
    /// <returns><see langword="true"/> if <paramref name="theme"/> is a variant of the family; otherwise, <see langword="false"/>.</returns>
    public bool Contains(Theme theme) => Equals(Light, theme) || Equals(Dark, theme);
}
