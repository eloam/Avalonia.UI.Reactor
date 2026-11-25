// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.Panels;
using MartletUI.Theming;
using MartletUI.Views;

namespace MartletUI.Reconciliation;

/// <summary>
/// Displays <c>ZStack</c>, <c>Overlay</c> and the modifier wrapper of a component with a <see cref="LayerStack"/>.
/// </summary>
/// <remarks>
/// The layers are as flexible as their content: a child that fills or has a share of space gives it to them.
/// </remarks>
internal sealed class ZStackAdapter : ViewAdapter<ZStackView, LayerStack>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly ZStackAdapter Instance = new();

    /// <inheritdoc/>
    public override float GrowOf(Node node) => node.Children.Select(c => c.Grow).DefaultIfEmpty(0).Max();

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, LayerStack layers, ZStackView view, ViewEnvironment environment)
    {
        layers.Alignment = view.Alignment;
        ViewEnvironment children = environment with
        {
            Font = environment.FontFor(view),
            Foreground = environment.ForegroundFor(view),
        };
        ControlTree.Arrange(layers.Children, reconciler.ReconcileChildren(node, view.Children, children));
    }

    /// <inheritdoc/>
    protected override (bool Width, bool Height) FlexOf(Node node, ZStackView view) =>
        (node.Children.Any(c => c.FillWidth), node.Children.Any(c => c.FillHeight));
}
