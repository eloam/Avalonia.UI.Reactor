// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents the look of text fields: colors for each state, dimensions, caret, selection and context menu.
/// </summary>
/// <param name="Background">The background of the field.</param>
/// <param name="Foreground">The color of the text.</param>
/// <param name="Placeholder">The color of the placeholder text, shown while the field is empty.</param>
/// <param name="BorderColor">The border at rest.</param>
/// <param name="HoverBorderColor">The border under the pointer.</param>
/// <param name="FocusBorderColor">The border while typing.</param>
/// <param name="Caret">The color of the caret.</param>
/// <param name="Padding">The space between the border and the text.</param>
/// <param name="Radius">The corner radius.</param>
/// <param name="BorderWidth">The border thickness at rest.</param>
/// <param name="FocusBorderWidth">The border thickness while typing; the text does not move.</param>
public sealed record TextFieldTheme(
    ColorValue Background,
    ColorValue Foreground,
    ColorValue Placeholder,
    ColorValue BorderColor,
    ColorValue HoverBorderColor,
    ColorValue FocusBorderColor,
    ColorValue Caret,
    Thickness Padding,
    float Radius,
    float BorderWidth = 1,
    float FocusBorderWidth = 2)
{
    /// <summary>
    /// Gets the standard text field look.
    /// </summary>
    public static readonly TextFieldTheme Default = new(
        Background: ColorRole.Surface,
        Foreground: ColorRole.Text,
        Placeholder: ColorRole.TextMuted,
        BorderColor: ColorRole.FieldBorder,
        HoverBorderColor: ColorRole.TextMuted,
        FocusBorderColor: ColorRole.Accent,
        Caret: ColorRole.Text,
        Padding: new(10, 7),
        Radius: 6);

    /// <summary>
    /// Gets the natural width of a field, before <c>.Grow()</c> or <c>.FillWidth()</c>.
    /// </summary>
    public float MinWidth { get; init; } = 160;

    /// <summary>
    /// Gets the half-period of the caret blink, in milliseconds; 0 for a steady caret.
    /// </summary>
    public int CaretBlinkMs { get; init; } = 530;

    /// <summary>
    /// Gets the background color of selected text, drawn at the opacity <see cref="SelectionOpacity"/>.
    /// </summary>
    public ColorValue Selection { get; init; } = ColorRole.Accent;

    /// <summary>
    /// Gets the opacity of the selection background, from 0 to 1.
    /// </summary>
    public float SelectionOpacity { get; init; } = 0.3f;

    /// <summary>
    /// Gets the labels of the context menu of the field (cut, copy, paste).
    /// </summary>
    public EditMenuLabels EditMenu { get; init; } = EditMenuLabels.Default;

    /// <summary>
    /// Gets the custom look of every text field of the theme, or <see langword="null"/> for the standard look.
    /// </summary>
    /// <remarks>
    /// The template draws the frame and places <see cref="TextFieldState.Editor"/> where the text and the caret appear.
    /// </remarks>
    public Func<TextFieldState, View>? Template { get; init; }
}
