// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Menus;

/// <summary>
/// Represents the menu bar of a <c>Body</c>: drawn at the top of the window, or handed to the system when the platform
/// has a menu bar (the global menu bar of macOS).
/// </summary>
/// <remarks>
/// The same description feeds both bars; only the one that is displayed matters.
/// </remarks>
internal sealed class ReactorMenuBar : Menu
{
    /// <summary>
    /// Gets the bar handed to the system, while the bar is in a window.
    /// </summary>
    public NativeMenu SystemMenu { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the system menu bar displays the menus and runs the shortcuts itself; the drawn
    /// bar then takes neither space nor spacing.
    /// </summary>
    public bool UsesSystemBar { get; private set; }

    /// <summary>
    /// Gets the type whose styles apply to the bar: it looks like a <see cref="Menu"/>.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(Menu);

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is not { } window)
        {
            return;
        }

        NativeMenu.SetMenu(window, SystemMenu);
        UsesSystemBar = NativeMenu.GetIsNativeMenuExported(window);
        IsVisible = !UsesSystemBar;
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (e.RootVisual is TopLevel window && ReferenceEquals(NativeMenu.GetMenu(window), SystemMenu))
        {
            NativeMenu.SetMenu(window, null);
        }

        UsesSystemBar = false;
    }
}
