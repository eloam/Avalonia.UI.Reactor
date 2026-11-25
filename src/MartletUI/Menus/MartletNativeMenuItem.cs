// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using MartletUI.Views;

namespace MartletUI.Menus;

/// <summary>
/// Represents an item of the system menu bar (macOS); it keeps the view it displays, whose action its click runs.
/// </summary>
internal sealed class MartletNativeMenuItem : NativeMenuItem
{
    /// <summary>
    /// Gets or sets the item displayed, or <see langword="null"/> for the title of a menu or a submenu.
    /// </summary>
    public MenuItemView? Entry { get; set; }
}
