// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;

namespace MartletUI.Views;

/// <summary>
/// Represents a menu item: a label and the action run by a click, by its shortcut or from the keyboard.
/// </summary>
/// <param name="Label">The label; the letter after <c>&amp;</c> is the access key.</param>
/// <param name="Action">The action of the item, or <see langword="null"/>.</param>
public sealed record MenuItemView(string Label, Action? Action) : View
{
    /// <summary>
    /// Gets the keyboard shortcut, active even when the menus are closed (<c>.Shortcut("Ctrl+N")</c>).
    /// </summary>
    public KeyGesture? Gesture { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item is enabled; a disabled item is dimmed, and neither its click nor its
    /// shortcut runs the action.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a check mark is shown before the label, or <see langword="null"/> for an item
    /// without a check mark.
    /// </summary>
    public bool? IsChecked { get; init; }
}
