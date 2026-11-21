// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents the look of checkboxes: colors for each state and dimensions.
/// </summary>
/// <param name="Background">The background of an unchecked box.</param>
/// <param name="BorderColor">The border of an unchecked box.</param>
/// <param name="HoverBorderColor">The border of an unchecked box under the pointer.</param>
/// <param name="CheckedBackground">The background of a checked box.</param>
/// <param name="CheckMark">The color of the check mark.</param>
/// <param name="Size">The side of the box.</param>
/// <param name="Radius">The corner radius of the box.</param>
/// <param name="BorderWidth">The border thickness of the box.</param>
/// <param name="LabelSpacing">The space between the box and its label.</param>
public sealed record CheckboxTheme(
    ColorValue Background,
    ColorValue BorderColor,
    ColorValue HoverBorderColor,
    ColorValue CheckedBackground,
    ColorValue CheckMark,
    float Size = 18,
    float Radius = 4,
    float BorderWidth = 1.5f,
    float LabelSpacing = 8)
{
    /// <summary>
    /// Gets the standard checkbox look.
    /// </summary>
    public static readonly CheckboxTheme Default = new(
        Background: ColorRole.Surface,
        BorderColor: ColorRole.FieldBorder,
        HoverBorderColor: ColorRole.Accent,
        CheckedBackground: ColorRole.Accent,
        CheckMark: ColorRole.OnAccent);

    /// <summary>
    /// Gets the thickness of the check mark for an 18-unit box; it scales with <see cref="Size"/>.
    /// </summary>
    public float CheckThickness { get; init; } = 2;

    /// <summary>
    /// Gets the opacity of the overlay on a checked box under the pointer.
    /// </summary>
    public float HoverOverlay { get; init; } = 0.1f;

    /// <summary>
    /// Gets the custom look of every checkbox of the theme, or <see langword="null"/> for the standard look.
    /// </summary>
    public Func<CheckboxState, View>? Template { get; init; }
}
