// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;
using MartletUI.State;
using MartletUI.Theming;

namespace MartletUI.Views;

/// <summary>
/// Represents an immutable description of a part of the user interface, produced by the <c>Body</c> of a component.
/// </summary>
/// <remarks>
/// The engine turns views into controls. Properties left <see langword="null"/> take the value of the current theme.
/// Views are created by the factories of <see cref="Ui"/> and refined with the modifiers of <see cref="Modifiers"/>.
/// </remarks>
public abstract record View
{
    /// <summary>
    /// Gets the stable identity of the view in a list; it keeps the control, focus and hover when the order changes.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets the inner spacing, or <see langword="null"/> for none.
    /// </summary>
    public Thickness? Padding { get; init; }

    /// <summary>
    /// Gets the width: <see langword="null"/> for the natural width, positive infinity to fill the available width.
    /// </summary>
    public float? Width { get; init; }

    /// <summary>
    /// Gets the height: <see langword="null"/> for the natural height, positive infinity to fill the available height.
    /// </summary>
    public float? Height { get; init; }

    /// <summary>
    /// Gets the share of the remaining space taken on the main axis of the parent stack; 0 for none.
    /// </summary>
    public float Grow { get; init; }

    /// <summary>
    /// Gets the alignment of the view on the cross axis of its parent stack, or <see langword="null"/> for the stack's.
    /// </summary>
    public Align? SelfAlign { get; init; }

    /// <summary>
    /// Gets the background color, or <see langword="null"/> for none.
    /// </summary>
    public ColorValue? Background { get; init; }

    /// <summary>
    /// Gets the border color, or <see langword="null"/> for no border.
    /// </summary>
    public ColorValue? BorderColor { get; init; }

    /// <summary>
    /// Gets the border thickness, or <see langword="null"/> for the default.
    /// </summary>
    public float? BorderWidth { get; init; }

    /// <summary>
    /// Gets the corner radius, or <see langword="null"/> for the default.
    /// </summary>
    public float? CornerRadius { get; init; }

    /// <summary>
    /// Gets the text color, inherited by the descendants, or <see langword="null"/> to inherit it.
    /// </summary>
    public ColorValue? Foreground { get; init; }

    /// <summary>
    /// Gets the text style, inherited by the descendants, or <see langword="null"/> to inherit it.
    /// </summary>
    public TextStyle? TextStyle { get; init; }

    /// <summary>
    /// Gets the name of a text style of the theme (<c>.Font("caption")</c>); it takes precedence over
    /// <see cref="TextStyle"/>.
    /// </summary>
    public string? TextStyleName { get; init; }

    /// <summary>
    /// Gets the font size, inherited by the descendants, or <see langword="null"/> to inherit it.
    /// </summary>
    public float? FontSize { get; init; }

    /// <summary>
    /// Gets a value indicating whether the text is bold, or <see langword="null"/> to inherit it.
    /// </summary>
    public bool? Bold { get; init; }

    /// <summary>
    /// Gets the exact theme applied to the view and its descendants, like the environment of SwiftUI.
    /// </summary>
    public Theme? Theme { get; init; }

    /// <summary>
    /// Gets the name of a family (or theme) of the catalog applied to the branch: <c>.Theme("Sable")</c>, light or dark
    /// depending on the mode.
    /// </summary>
    public string? ThemeName { get; init; }

    /// <summary>
    /// Gets the theme family applied to the branch, light or dark depending on the mode.
    /// </summary>
    public ThemeFamily? ThemeFamily { get; init; }

    /// <summary>
    /// Gets the color scheme forced on the branch: the matching variant of the current family.
    /// </summary>
    public ColorScheme? Scheme { get; init; }

    /// <summary>
    /// Gets the action run when the view is clicked, or <see langword="null"/> if the view is not clickable.
    /// </summary>
    public Action? TapAction { get; init; }

    /// <summary>
    /// Gets the action run once when the view appears, or <see langword="null"/>.
    /// </summary>
    public Action? AppearAction { get; init; }

    /// <summary>
    /// Gets the action run once when the view disappears, or <see langword="null"/>.
    /// </summary>
    public Action? DisappearAction { get; init; }

    /// <summary>
    /// Gets the task run while the view is displayed, cancelled when it disappears, or <see langword="null"/>.
    /// </summary>
    public Func<CancellationToken, Task>? TaskAction { get; init; }

    /// <summary>
    /// Gets the value whose change cancels the task and starts it again, or <see langword="null"/>.
    /// </summary>
    public object? TaskKey { get; init; }

    /// <summary>
    /// Gets the property that a bound control reads and writes, which identifies the control among its siblings, or
    /// <see langword="null"/>.
    /// </summary>
    internal BindingTarget? BindingTarget { get; init; }
}
