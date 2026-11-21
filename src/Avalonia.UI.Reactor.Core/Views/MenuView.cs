// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents a menu: a title and its items.
/// </summary>
/// <param name="Title">The title; in <c>"&amp;File"</c>, the letter after <c>&amp;</c> opens the menu with Alt.</param>
/// <param name="Items">The items: <see cref="MenuItemView"/>, submenus (<see cref="MenuView"/>) and separators
/// (<c>Divider()</c>).</param>
public sealed record MenuView(string Title, IReadOnlyList<View> Items) : View;
