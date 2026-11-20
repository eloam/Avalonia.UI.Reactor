namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary><c>Canvas</c> : un <see cref="DrawingCanvas"/>, redessiné à chaque nouvelle version de la vue.</summary>
internal sealed class CanvasAdapter : ViewAdapter<CanvasView, DrawingCanvas>
{
    public static readonly CanvasAdapter Instance = new();

    protected override void Update(Reconciler reconciler, Node node, DrawingCanvas canvas, CanvasView view, ViewEnvironment env) =>
        canvas.Update(view.Draw, env.Theme, env.ForegroundFor(view), env.FontFor(view));
}
