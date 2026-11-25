// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using MartletUI.Views;

namespace MartletUI.Menus;

/// <summary>
/// Represents an item of the bar drawn in the window; it keeps the view it displays, whose action its click runs.
/// </summary>
internal sealed class MartletMenuItem : MenuItem
{
    /// <summary>
    /// Gets or sets the item displayed, or <see langword="null"/> for the title of a menu or a submenu.
    /// </summary>
    public MenuItemView? Entry { get; set; }

    /// <summary>
    /// Gets the type whose styles apply to the item: it looks like a <see cref="MenuItem"/>.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(MenuItem);
}
