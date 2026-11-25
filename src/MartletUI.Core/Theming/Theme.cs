// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;

namespace MartletUI.Theming;

/// <summary>
/// Represents a complete theme, identified by its name.
/// </summary>
/// <remarks>
/// A theme is an immutable record: start from an existing theme and change only what differs.
/// </remarks>
/// <example>
/// <code>
/// var sable = Theme.Light.Named("Sable")
///     .WithColor(ColorRole.Page, "#F6EFE6")
///     .WithColor(ColorRole.Accent, "#B45309")
///     .WithColor("success", "#15803D")          // a color of your own, used with .Background("success")
///     .WithFont("Georgia")
///     .WithRadius(12);
/// </code>
/// </example>
/// <param name="Name">The name of the theme.</param>
/// <param name="Scheme">Whether the theme is light or dark.</param>
/// <param name="Palette">The colors of the theme.</param>
/// <param name="Typography">The font and the text styles of the theme.</param>
/// <param name="Button">The look of buttons.</param>
/// <param name="TextField">The look of text fields.</param>
/// <param name="Checkbox">The look of checkboxes.</param>
public sealed record Theme(
    string Name,
    ColorScheme Scheme,
    Palette Palette,
    Typography Typography,
    ButtonTheme Button,
    TextFieldTheme TextField,
    CheckboxTheme Checkbox)
{
    /// <summary>
    /// Gets the built-in light theme.
    /// </summary>
    public static readonly Theme Light = From("Light", ColorScheme.Light, Palette.Light);

    /// <summary>
    /// Gets the built-in dark theme.
    /// </summary>
    public static readonly Theme Dark = From("Dark", ColorScheme.Dark, Palette.Dark);

    /// <summary>
    /// Gets the default spacing between the children of <c>VStack</c> and <c>HStack</c>.
    /// </summary>
    public float Spacing { get; init; } = 8;

    /// <summary>
    /// Gets the color of <c>Divider</c> lines.
    /// </summary>
    public ColorValue DividerColor { get; init; } = ColorRole.Border;

    /// <summary>
    /// Gets the thickness of <c>Divider</c> lines.
    /// </summary>
    public float DividerThickness { get; init; } = 1;

    /// <summary>
    /// Gets the opacity of the overlay on a clickable view (<c>.OnTap</c>) under the pointer.
    /// </summary>
    public float HoverOverlay { get; init; } = 0.04f;

    /// <summary>
    /// Gets the opacity of the overlay on a clickable view (<c>.OnTap</c>) while it is pressed.
    /// </summary>
    public float PressedOverlay { get; init; } = 0.08f;

    /// <summary>
    /// Gets the look of the drawn menu bar.
    /// </summary>
    public MenuTheme Menu { get; init; } = MenuTheme.Default;

    /// <summary>
    /// Gets the color of a value in this theme.
    /// </summary>
    /// <param name="color">A fixed color, a role or a named color.</param>
    /// <returns>The resolved color.</returns>
    public Color this[ColorValue color] => color.Resolve(Palette);

    /// <summary>
    /// Creates a theme whose control styles all derive from its palette.
    /// </summary>
    /// <param name="name">The name of the theme.</param>
    /// <param name="scheme">Whether the theme is light or dark.</param>
    /// <param name="palette">The colors of the theme.</param>
    /// <param name="typography">The font and text styles, or <see langword="null"/> for the defaults.</param>
    /// <returns>The new theme.</returns>
    public static Theme From(string name, ColorScheme scheme, Palette palette, Typography? typography = null) =>
        new(name, scheme, palette, typography ?? new Typography(), ButtonTheme.Default, TextFieldTheme.Default, CheckboxTheme.Default);

    /// <summary>
    /// Returns a copy of this theme under another name: the starting point of a theme of your own.
    /// </summary>
    /// <param name="name">The name of the new theme.</param>
    /// <returns>The renamed copy.</returns>
    public Theme Named(string name) => this with { Name = name };

    /// <summary>
    /// Returns a copy of this theme with a changed palette.
    /// </summary>
    /// <param name="change">A function that receives the current palette and returns the new one.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithPalette(Func<Palette, Palette> change) => this with { Palette = change(Palette) };

    /// <summary>
    /// Returns a copy of this theme with a changed typography.
    /// </summary>
    /// <param name="change">A function that receives the current typography and returns the new one.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithTypography(Func<Typography, Typography> change) => this with { Typography = change(Typography) };

    /// <summary>
    /// Returns a copy of this theme with a changed button look.
    /// </summary>
    /// <param name="change">A function that receives the current button look and returns the new one.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithButton(Func<ButtonTheme, ButtonTheme> change) => this with { Button = change(Button) };

    /// <summary>
    /// Returns a copy of this theme with a changed text field look.
    /// </summary>
    /// <param name="change">A function that receives the current text field look and returns the new one.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithTextField(Func<TextFieldTheme, TextFieldTheme> change) => this with { TextField = change(TextField) };

    /// <summary>
    /// Returns a copy of this theme with a changed checkbox look.
    /// </summary>
    /// <param name="change">A function that receives the current checkbox look and returns the new one.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithCheckbox(Func<CheckboxTheme, CheckboxTheme> change) => this with { Checkbox = change(Checkbox) };

    /// <summary>
    /// Returns a copy of this theme with a changed menu bar look.
    /// </summary>
    /// <param name="change">A function that receives the current menu bar look and returns the new one.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithMenu(Func<MenuTheme, MenuTheme> change) => this with { Menu = change(Menu) };

    /// <summary>
    /// Returns a copy of this theme where a role has a new color.
    /// </summary>
    /// <param name="role">The role to change.</param>
    /// <param name="color">The new color.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithColor(ColorRole role, Color color) => WithPalette(p => p.With(role, color));

    /// <summary>
    /// Returns a copy of this theme with a color added by name, used afterwards as <c>.Foreground("success")</c>.
    /// </summary>
    /// <param name="name">The name of the color, or of a role to change.</param>
    /// <param name="color">The color.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithColor(string name, Color color) => WithPalette(p => p.With(name, color));

    /// <summary>
    /// Returns a copy of this theme with another accent color.
    /// </summary>
    /// <param name="accent">The accent color.</param>
    /// <param name="onAccent">The color of text on the accent color, or <see langword="null"/> to keep the current one.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithAccent(Color accent, Color? onAccent = null) =>
        WithPalette(p => p with { Accent = accent, OnAccent = onAccent ?? p.OnAccent });

    /// <summary>
    /// Returns a copy of this theme with another font family and, optionally, another body text size.
    /// </summary>
    /// <param name="family">The font family, installed on the system.</param>
    /// <param name="bodySize">The size of body text, or <see langword="null"/> to keep the current one.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithFont(string family, float? bodySize = null) =>
        WithTypography(t => t with { Family = family, Body = bodySize is { } size ? t.Body with { Size = size } : t.Body });

    /// <summary>
    /// Returns a copy of this theme with a text style added or replaced, used with <c>.Font("caption")</c>.
    /// </summary>
    /// <param name="name">The name of the style.</param>
    /// <param name="size">The font size.</param>
    /// <param name="weight">The font weight, on the OpenType scale.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithTextStyle(string name, float size, int weight = FontWeight.Regular) =>
        WithTypography(t => t.With(name, new TextStyleSpec(size, weight)));

    /// <summary>
    /// Returns a copy of this theme with a button style added or replaced, used with <c>.Style("success")</c>.
    /// </summary>
    /// <param name="name">The name of the style.</param>
    /// <param name="variant">The colors of the style.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithButtonStyle(string name, ButtonVariant variant) => WithButton(b => b.With(name, variant));

    /// <summary>
    /// Returns a copy of this theme with the same corner radius for buttons, text fields and checkboxes.
    /// </summary>
    /// <param name="radius">The corner radius; checkboxes are limited to a circle.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithRadius(float radius) => this with
    {
        Button = Button with { Radius = radius },
        TextField = TextField with { Radius = radius },
        Checkbox = Checkbox with { Radius = Math.Min(radius, Checkbox.Size / 2) },
    };

    /// <summary>
    /// Returns a copy of this theme with another default stack spacing.
    /// </summary>
    /// <param name="spacing">The space between the children of a stack.</param>
    /// <returns>The updated theme.</returns>
    public Theme WithSpacing(float spacing) => this with { Spacing = spacing };

    /// <summary>
    /// Returns a background with the hover or pressed overlay: the text color of the theme, translucent, which darkens a
    /// light theme and lightens a dark one without per-theme settings. On a transparent background, the overlay alone.
    /// </summary>
    /// <param name="background">The background color.</param>
    /// <param name="amount">The opacity of the overlay, from 0 to 1.</param>
    /// <returns>The background with the overlay.</returns>
    internal Color Overlay(Color background, float amount) =>
        amount <= 0 ? background
        : background.A == 0 ? Palette.Text.WithAlpha(amount)
        : background.Mix(Palette.Text with { A = background.A }, amount);
}
