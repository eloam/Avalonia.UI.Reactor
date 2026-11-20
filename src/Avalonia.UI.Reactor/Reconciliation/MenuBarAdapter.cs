using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Menus;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// <c>MenuBar(…)</c> : une <see cref="ReactorMenuBar"/>, dessinée en haut de la fenêtre aux couleurs du thème
/// (<c>MenuTheme</c>), ou confiée à la barre du système. L'hôte en lit les raccourcis.
/// </summary>
internal sealed class MenuBarAdapter : ViewAdapter<MenuBarView, ReactorMenuBar>
{
    public static readonly MenuBarAdapter Instance = new();

    public override bool DrawsDecoration => true;

    protected override void Attach(Reconciler reconciler, Node node, ReactorMenuBar bar)
    {
        bar.AttachedToVisualTree += (_, _) => reconciler.MenuBar = node;
        bar.DetachedFromVisualTree += (_, _) =>
        {
            if (reconciler.MenuBar == node) reconciler.MenuBar = null;
        };
    }

    protected override void Update(Reconciler reconciler, Node node, ReactorMenuBar bar, MenuBarView view, ViewEnvironment env)
    {
        var theme = env.Theme;
        var style = theme.Menu;
        bar.SetFont(env.FontFor(view));
        bar.SetBrush(TemplatedControl.BackgroundProperty, theme[view.Background ?? style.BarBackground]);
        bar.SetBrush(TemplatedControl.ForegroundProperty, env.ForegroundFor(view));
        bar.SetBrush(TemplatedControl.BorderBrushProperty, theme[view.BorderColor ?? style.BarBorderColor]);
        bar.BorderThickness = new Thickness(0, 0, 0, view.BorderWidth ?? style.BarBorderWidth);
        bar.Padding = (view.Padding ?? style.BarPadding).ToAvalonia();
        MenuSync.Apply(bar.Items, view.Menus, reconciler.Run);
        MenuSync.Apply(bar.SystemMenu.Items, view.Menus, reconciler.Run);
    }

    /// <summary>La barre occupe toute la largeur dès qu'elle a des menus.</summary>
    protected override (bool Width, bool Height) FlexOf(Node node, MenuBarView view) => (view.Menus.Count > 0, false);
}
