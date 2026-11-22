// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Gives the control of a view its size and its flexibility, which its container reads to share its space.
/// </summary>
internal static class Placement
{
    /// <summary>
    /// Sets the size and the flexibility of the control of a node.
    /// </summary>
    /// <remarks>
    /// An explicit size wins (<c>.Width(72)</c>, or infinity to fill); otherwise a container is as flexible as its
    /// children.
    /// </remarks>
    /// <param name="node">The node of the view.</param>
    /// <param name="view">The view.</param>
    public static void Place(Node node, View view)
    {
        (bool fillWidth, bool fillHeight) = node.Adapter.FlexOf(node, view);
        node.FillWidth = view.Width is { } width ? float.IsPositiveInfinity(width) : fillWidth;
        node.FillHeight = view.Height is { } height ? float.IsPositiveInfinity(height) : fillHeight;
        node.Grow = view.Grow > 0 ? view.Grow : node.Adapter.GrowOf(node);

        Control root = node.Root;
        root.Width = view.Width is { } fixedWidth && !float.IsInfinity(fixedWidth) ? fixedWidth : double.NaN;
        root.Height = view.Height is { } fixedHeight && !float.IsInfinity(fixedHeight) ? fixedHeight : double.NaN;
        if (node.Decoration is not null)
        {
            node.Content.Width = double.NaN;
            node.Content.Height = double.NaN;
        }

        Flex.SetGrow(root, node.Grow);
        Flex.SetFillWidth(root, node.FillWidth);
        Flex.SetFillHeight(root, node.FillHeight);
        Flex.SetSelfAlign(root, view.SelfAlign);
    }
}
