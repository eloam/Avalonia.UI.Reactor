// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;

namespace MartletUI.Reconciliation;

/// <summary>
/// Displays a <c>Box()</c> with an empty <see cref="Surface"/>, made of its modifiers only (size, background, border,
/// corners, click).
/// </summary>
internal sealed class BoxAdapter : ViewAdapter<BoxView, Surface>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly BoxAdapter Instance = new();

    /// <inheritdoc/>
    public override bool DrawsDecoration => true;

    /// <inheritdoc/>
    protected override void Attach(Reconciler reconciler, Node node, Surface box) =>
        box.TapAction = () => reconciler.Run(node.View.TapAction);

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, Surface box, BoxView view, ViewEnvironment environment) =>
        Decorations.Paint(box, view, environment.Theme);
}
