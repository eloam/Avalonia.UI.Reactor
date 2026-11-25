// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;
using MartletUI.Views;

namespace MartletUI.Theming;

/// <summary>
/// Represents the look of buttons: the four built-in styles (<see cref="ButtonStyle"/>) and styles added by name.
/// </summary>
/// <remarks>
/// A style added with <c>theme.WithButtonStyle("success", variant)</c> is used with <c>.Style("success")</c>.
/// </remarks>
/// <param name="Primary">The colors of the <see cref="ButtonStyle.Primary"/> style.</param>
/// <param name="Secondary">The colors of the <see cref="ButtonStyle.Secondary"/> style.</param>
/// <param name="Ghost">The colors of the <see cref="ButtonStyle.Ghost"/> style.</param>
/// <param name="Danger">The colors of the <see cref="ButtonStyle.Danger"/> style.</param>
/// <param name="Padding">The space between the border and the label.</param>
/// <param name="Radius">The corner radius.</param>
/// <param name="BorderWidth">The border thickness, for styles that have a border.</param>
/// <param name="FontSize">The font size of the label, or <see langword="null"/> for the body text size.</param>
/// <param name="Bold">A value indicating whether the label is bold.</param>
public sealed record ButtonTheme(
    ButtonVariant Primary,
    ButtonVariant Secondary,
    ButtonVariant Ghost,
    ButtonVariant Danger,
    Thickness Padding,
    float Radius,
    float BorderWidth = 1,
    float? FontSize = null,
    bool Bold = false)
{
    /// <summary>
    /// Gets the standard button look.
    /// </summary>
    public static readonly ButtonTheme Default = new(
        Primary: new(ColorRole.Accent, ColorRole.OnAccent),
        Secondary: new(ColorRole.Secondary, ColorRole.OnSecondary),
        Ghost: new(Color.Transparent, ColorRole.TextMuted, HoverOverlay: 0.06f, PressedOverlay: 0.12f),
        Danger: new(ColorRole.Danger, ColorRole.OnDanger),
        Padding: new(14, 7),
        Radius: 6);

    /// <summary>
    /// Gets the custom look of every button of the theme, or <see langword="null"/> for the standard look.
    /// </summary>
    /// <remarks>
    /// The template receives the label, the colors of the style and the state (hover, pressed), and returns any view.
    /// </remarks>
    public Func<ButtonState, View>? Template { get; init; }

    /// <summary>
    /// Gets the styles added by name; names are case-insensitive.
    /// </summary>
    public IReadOnlyDictionary<string, ButtonVariant> Custom { get; init; } =
        new Dictionary<string, ButtonVariant>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the colors of a built-in style.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <returns>The colors of the style.</returns>
    public ButtonVariant this[ButtonStyle style] => style switch
    {
        ButtonStyle.Secondary => Secondary,
        ButtonStyle.Ghost => Ghost,
        ButtonStyle.Danger => Danger,
        _ => Primary,
    };

    /// <summary>
    /// Gets the colors of a style by name: a built-in style (<c>"ghost"</c>…) or an added one.
    /// </summary>
    /// <param name="name">The name of the style, case-insensitive.</param>
    /// <returns>The colors of the style, or those of the primary style if the name is unknown.</returns>
    public ButtonVariant this[string name] =>
        Enum.TryParse(name, ignoreCase: true, out ButtonStyle style) ? this[style]
        : Custom.TryGetValue(name, out ButtonVariant? variant) ? variant
        : Primary;

    /// <summary>
    /// Returns a copy of this theme where a style has new colors.
    /// </summary>
    /// <param name="name">The name of a built-in style, or of a style to add or replace.</param>
    /// <param name="variant">The colors of the style.</param>
    /// <returns>The updated theme.</returns>
    public ButtonTheme With(string name, ButtonVariant variant)
    {
        if (!Enum.TryParse(name, ignoreCase: true, out ButtonStyle style))
        {
            return this with
            {
                Custom = new Dictionary<string, ButtonVariant>(Custom, StringComparer.OrdinalIgnoreCase) { [name] = variant },
            };
        }

        return style switch
        {
            ButtonStyle.Secondary => this with { Secondary = variant },
            ButtonStyle.Ghost => this with { Ghost = variant },
            ButtonStyle.Danger => this with { Danger = variant },
            _ => this with { Primary = variant },
        };
    }
}
