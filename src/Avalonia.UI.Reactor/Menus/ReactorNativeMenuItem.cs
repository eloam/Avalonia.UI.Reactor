// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Menus;

using Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents an item of the system menu bar (macOS); it keeps the view it displays, whose action its click runs.
/// </summary>
internal sealed class ReactorNativeMenuItem : NativeMenuItem
{
    /// <summary>
    /// Gets or sets the item displayed, or <see langword="null"/> for the title of a menu or a submenu.
    /// </summary>
    public MenuItemView? Entry { get; set; }
}
