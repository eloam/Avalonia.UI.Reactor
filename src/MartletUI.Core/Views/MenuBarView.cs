// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Views;

/// <summary>
/// Represents the menu bar of the window.
/// </summary>
/// <remarks>
/// The bar is drawn at the top of the window, or handed to the system menu bar when the platform has one (macOS); the
/// code of the application does not change.
/// </remarks>
/// <param name="Menus">The menus of the bar.</param>
public sealed record MenuBarView(IReadOnlyList<MenuView> Menus) : View;
