// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls.Primitives;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Menus;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Displays <c>MenuBar(…)</c> with a <see cref="ReactorMenuBar"/>, drawn at the top of the window in the colors of the
/// theme (<see cref="MenuTheme"/>), or handed to the system menu bar. The host reads its shortcuts.
/// </summary>
internal sealed class MenuBarAdapter : ViewAdapter<MenuBarView, ReactorMenuBar>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly MenuBarAdapter Instance = new();

    /// <inheritdoc/>
    public override bool DrawsDecoration => true;

    /// <inheritdoc/>
    protected override void Attach(Reconciler reconciler, Node node, ReactorMenuBar bar)
    {
        bar.AttachedToVisualTree += (_, _) => reconciler.MenuBar = node;
        bar.DetachedFromVisualTree += (_, _) =>
        {
            if (reconciler.MenuBar == node)
            {
                reconciler.MenuBar = null;
            }
        };
    }

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, ReactorMenuBar bar, MenuBarView view, ViewEnvironment environment)
    {
        Theme theme = environment.Theme;
        MenuTheme style = theme.Menu;
        bar.SetFont(environment.FontFor(view));
        bar.SetBrush(TemplatedControl.BackgroundProperty, theme[view.Background ?? style.BarBackground]);
        bar.SetBrush(TemplatedControl.ForegroundProperty, environment.ForegroundFor(view));
        bar.SetBrush(TemplatedControl.BorderBrushProperty, theme[view.BorderColor ?? style.BarBorderColor]);
        bar.BorderThickness = new Thickness(0, 0, 0, view.BorderWidth ?? style.BarBorderWidth);
        bar.Padding = (view.Padding ?? style.BarPadding).ToAvalonia();
        MenuSync.Apply(bar.Items, view.Menus, reconciler.Run);
        MenuSync.Apply(bar.SystemMenu.Items, view.Menus, reconciler.Run);
    }

    /// <summary>
    /// Returns the flexibility of the bar: it takes the whole width as soon as it has menus.
    /// </summary>
    /// <param name="node">The node of the bar.</param>
    /// <param name="view">The view of the bar.</param>
    /// <returns>Whether the bar fills the available width and height.</returns>
    protected override (bool Width, bool Height) FlexOf(Node node, MenuBarView view) => (view.Menus.Count > 0, false);
}
