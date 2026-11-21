// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents the colors of a theme: the built-in roles (<see cref="ColorRole"/>) and colors added by name.
/// </summary>
/// <remarks>
/// A color added with <c>palette.With("success", "#15803D")</c> is used with <c>.Background("success")</c>.
/// </remarks>
/// <param name="Page">The color of the <see cref="ColorRole.Page"/> role.</param>
/// <param name="Surface">The color of the <see cref="ColorRole.Surface"/> role.</param>
/// <param name="Border">The color of the <see cref="ColorRole.Border"/> role.</param>
/// <param name="FieldBorder">The color of the <see cref="ColorRole.FieldBorder"/> role.</param>
/// <param name="Text">The color of the <see cref="ColorRole.Text"/> role.</param>
/// <param name="TextMuted">The color of the <see cref="ColorRole.TextMuted"/> role.</param>
/// <param name="Accent">The color of the <see cref="ColorRole.Accent"/> role.</param>
/// <param name="OnAccent">The color of the <see cref="ColorRole.OnAccent"/> role.</param>
/// <param name="Secondary">The color of the <see cref="ColorRole.Secondary"/> role.</param>
/// <param name="OnSecondary">The color of the <see cref="ColorRole.OnSecondary"/> role.</param>
/// <param name="Danger">The color of the <see cref="ColorRole.Danger"/> role.</param>
/// <param name="OnDanger">The color of the <see cref="ColorRole.OnDanger"/> role.</param>
public sealed record Palette(
    Color Page,
    Color Surface,
    Color Border,
    Color FieldBorder,
    Color Text,
    Color TextMuted,
    Color Accent,
    Color OnAccent,
    Color Secondary,
    Color OnSecondary,
    Color Danger,
    Color OnDanger)
{
    /// <summary>
    /// Gets the color displayed for a name the theme does not know: magenta, so that the mistake shows immediately.
    /// </summary>
    public static readonly Color Missing = new(255, 0, 255);

    /// <summary>
    /// Gets the palette of the built-in light theme.
    /// </summary>
    public static readonly Palette Light = new(
        Page: "#F3F4F6",
        Surface: "#FFFFFF",
        Border: "#E5E7EB",
        FieldBorder: "#D1D5DB",
        Text: "#111827",
        TextMuted: "#6B7280",
        Accent: "#2563EB",
        OnAccent: "#FFFFFF",
        Secondary: "#E5E7EB",
        OnSecondary: "#111827",
        Danger: "#DC2626",
        OnDanger: "#FFFFFF");

    /// <summary>
    /// Gets the palette of the built-in dark theme.
    /// </summary>
    public static readonly Palette Dark = new(
        Page: "#0B0F17",
        Surface: "#161B26",
        Border: "#262D3B",
        FieldBorder: "#384152",
        Text: "#F3F4F6",
        TextMuted: "#9CA3AF",
        Accent: "#3B82F6",
        OnAccent: "#FFFFFF",
        Secondary: "#262D3B",
        OnSecondary: "#F3F4F6",
        Danger: "#EF4444",
        OnDanger: "#FFFFFF");

    /// <summary>
    /// Gets the colors added by name; names are case-insensitive.
    /// </summary>
    public IReadOnlyDictionary<string, Color> Custom { get; init; } =
        new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the color of a role.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <returns>The color of the role.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="role"/> is not a defined role.</exception>
    public Color this[ColorRole role] => role switch
    {
        ColorRole.Page => Page,
        ColorRole.Surface => Surface,
        ColorRole.Border => Border,
        ColorRole.FieldBorder => FieldBorder,
        ColorRole.Text => Text,
        ColorRole.TextMuted => TextMuted,
        ColorRole.Accent => Accent,
        ColorRole.OnAccent => OnAccent,
        ColorRole.Secondary => Secondary,
        ColorRole.OnSecondary => OnSecondary,
        ColorRole.Danger => Danger,
        ColorRole.OnDanger => OnDanger,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    /// <summary>
    /// Gets a color by name: a role (<c>"accent"</c>, <c>"page"</c>…) or an added color.
    /// </summary>
    /// <param name="name">The name of the color, case-insensitive.</param>
    /// <returns>The color, or <see cref="Missing"/> if the name is unknown.</returns>
    public Color this[string name] =>
        Enum.TryParse(name, ignoreCase: true, out ColorRole role) ? this[role]
        : Custom.TryGetValue(name, out Color color) ? color
        : Missing;

    /// <summary>
    /// Returns a copy of this palette where a role has a new color.
    /// </summary>
    /// <param name="role">The role to change.</param>
    /// <param name="color">The new color.</param>
    /// <returns>The updated palette.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="role"/> is not a defined role.</exception>
    public Palette With(ColorRole role, Color color) => role switch
    {
        ColorRole.Page => this with { Page = color },
        ColorRole.Surface => this with { Surface = color },
        ColorRole.Border => this with { Border = color },
        ColorRole.FieldBorder => this with { FieldBorder = color },
        ColorRole.Text => this with { Text = color },
        ColorRole.TextMuted => this with { TextMuted = color },
        ColorRole.Accent => this with { Accent = color },
        ColorRole.OnAccent => this with { OnAccent = color },
        ColorRole.Secondary => this with { Secondary = color },
        ColorRole.OnSecondary => this with { OnSecondary = color },
        ColorRole.Danger => this with { Danger = color },
        ColorRole.OnDanger => this with { OnDanger = color },
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    /// <summary>
    /// Returns a copy of this palette where a role, or a color added by name, has a new color.
    /// </summary>
    /// <param name="name">The name of a role, or of a color to add or replace.</param>
    /// <param name="color">The new color.</param>
    /// <returns>The updated palette.</returns>
    public Palette With(string name, Color color)
    {
        if (Enum.TryParse(name, ignoreCase: true, out ColorRole role))
        {
            return With(role, color);
        }

        return this with
        {
            Custom = new Dictionary<string, Color>(Custom, StringComparer.OrdinalIgnoreCase) { [name] = color },
        };
    }
}
