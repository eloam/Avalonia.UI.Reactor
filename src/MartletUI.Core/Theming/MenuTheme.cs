// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;

namespace MartletUI.Theming;

/// <summary>
/// Represents the look of the menu bar drawn in the window: background, bottom line and padding.
/// </summary>
/// <remarks>
/// Open menus keep the look of Avalonia for now; the system menu bar (macOS) follows the system.
/// </remarks>
public sealed record MenuTheme
{
    /// <summary>
    /// Gets the standard menu bar look.
    /// </summary>
    public static readonly MenuTheme Default = new();

    /// <summary>
    /// Gets the background of the bar.
    /// </summary>
    public ColorValue BarBackground { get; init; } = ColorRole.Surface;

    /// <summary>
    /// Gets the color of the line that separates the bar from the content.
    /// </summary>
    public ColorValue BarBorderColor { get; init; } = ColorRole.Border;

    /// <summary>
    /// Gets the thickness of the line that separates the bar from the content.
    /// </summary>
    public float BarBorderWidth { get; init; } = 1;

    /// <summary>
    /// Gets the inner spacing of the bar.
    /// </summary>
    public Thickness BarPadding { get; init; } = new(4, 2);
}
