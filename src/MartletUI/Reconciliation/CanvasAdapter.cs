// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;

namespace MartletUI.Reconciliation;

/// <summary>
/// Displays a <c>Canvas</c> view with a <see cref="DrawingCanvas"/>, redrawn on each new version of the view.
/// </summary>
internal sealed class CanvasAdapter : ViewAdapter<CanvasView, DrawingCanvas>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly CanvasAdapter Instance = new();

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, DrawingCanvas canvas, CanvasView view, ViewEnvironment environment) =>
        canvas.Update(view.Draw, environment.Theme, environment.ForegroundFor(view), environment.FontFor(view));
}
