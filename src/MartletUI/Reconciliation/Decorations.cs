// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;

namespace MartletUI.Reconciliation;

/// <summary>
/// Gives a view its background, border, corners, padding and click when its control does not draw them itself.
/// </summary>
internal static class Decorations
{
    /// <summary>
    /// Wraps the control of a node in a <see cref="Surface"/> when its view needs one, and dresses the surface.
    /// </summary>
    /// <remarks>
    /// A view whose control does not draw its own decoration (text, stack, checkbox…), or a clickable view
    /// (<c>.OnTap</c>), gets a surface only when it needs one. The control moves into the surface, and out of it,
    /// without being recreated: it is detached, then attached again to the visual tree.
    /// </remarks>
    /// <param name="node">The node of the view.</param>
    /// <param name="view">The view.</param>
    /// <param name="theme">The theme of the view.</param>
    /// <param name="run">The function that runs the click of the view, then updates the interface.</param>
    public static void Decorate(Node node, View view, Theme theme, Action<Action?> run)
    {
        if (node.Content is Surface)
        {
            // A box is already a surface: its adapter dresses it.
            return;
        }

        bool decorated = view.Background is not null
            || view.BorderColor is not null
            || view.CornerRadius is not null
            || view.Padding is not null;
        bool needed = view.TapAction is not null || (decorated && !node.Adapter.DrawsDecoration);
        if (needed && node.Decoration is null)
        {
            ControlTree.Detach(node.Content);
            node.Decoration = new Surface { Child = node.Content, TapAction = () => run(node.View.TapAction) };
        }
        else if (!needed && node.Decoration is { } unused)
        {
            unused.Child = null;
            node.Decoration = null;
        }

        if (node.Decoration is not { } surface)
        {
            return;
        }

        if (node.Adapter.DrawsDecoration)
        {
            // The control keeps its own background; the surface only adds the click.
            surface.Paint(null, theme, tappable: true);
        }
        else
        {
            Paint(surface, view, theme);
        }
    }

    /// <summary>
    /// Sets the background, border, corners, padding and click of a view on the surface that carries them.
    /// </summary>
    /// <param name="surface">The surface.</param>
    /// <param name="view">The view.</param>
    /// <param name="theme">The theme of the view.</param>
    public static void Paint(Surface surface, View view, Theme theme)
    {
        surface.Paint(view.Background?.Resolve(theme.Palette), theme, tappable: view.TapAction is not null);
        surface.SetBrush(Border.BorderBrushProperty, view.BorderColor?.Resolve(theme.Palette));
        surface.BorderThickness = new Thickness(view.BorderColor is null ? 0 : view.BorderWidth ?? 1);
        surface.CornerRadius = new CornerRadius(view.CornerRadius ?? 0);
        surface.Padding = (view.Padding ?? default).ToAvalonia();
    }
}
