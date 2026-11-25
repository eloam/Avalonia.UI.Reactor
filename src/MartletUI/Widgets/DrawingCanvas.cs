// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MartletUI.Rendering;
using MartletUI.State;
using MartletUI.Theming;

namespace MartletUI.Widgets;

/// <summary>
/// Represents the control of a <c>Canvas</c> view: free drawing, clipped to the bounds of the control.
/// </summary>
/// <remarks>
/// The drawing function of the view receives a <see cref="DrawScope"/> (theme, inherited color and font) that draws on
/// the Avalonia drawing context. The drawing has its own scope: a state read while drawing makes it run again when that
/// state changes, without re-evaluating the view that contains the canvas.
/// </remarks>
internal sealed class DrawingCanvas : Control
{
    // The name of the scope of the drawing, which has no place in the tree of scopes.
    private const string DrawingScopeId = "canvas";

    private readonly Scope _drawing;
    private Action<DrawScope>? _draw;
    private Theme? _theme;
    private Primitives.Color _foreground;
    private Primitives.Font _font;
    private int _redrawPending;

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawingCanvas"/> class.
    /// </summary>
    public DrawingCanvas()
    {
        ClipToBounds = true;
        _drawing = new Scope(RequestRedraw, DrawingScopeId, identity: null);
    }

    /// <summary>
    /// Applies a new version of the view: its drawing runs again.
    /// </summary>
    /// <param name="draw">The drawing function of the view.</param>
    /// <param name="theme">The theme of the canvas.</param>
    /// <param name="foreground">The inherited text color.</param>
    /// <param name="font">The inherited font.</param>
    public void Update(Action<DrawScope> draw, Theme theme, Primitives.Color foreground, Primitives.Font font)
    {
        _draw = draw;
        _theme = theme;
        _foreground = foreground;
        _font = font;
        InvalidateVisual();
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        Interlocked.Exchange(ref _redrawPending, 0);
        if (_draw is not { } draw || _theme is null)
        {
            return;
        }

        using var drawing = new AvaloniaDrawingContext(context);
        var bounds = new Primitives.Rect(0, 0, (float)Bounds.Width, (float)Bounds.Height);
        var scope = new DrawScope(drawing, bounds, _theme, _foreground, _font);
        _drawing.Evaluate(() => draw(scope));
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // No subscription to state while the control is not displayed.
        _drawing.Release();
    }

    // A state read by the drawing changed, on any thread: redraw once, on the user interface thread.
    private void RequestRedraw()
    {
        if (Interlocked.Exchange(ref _redrawPending, 1) == 0)
        {
            Dispatcher.UIThread.Post(InvalidateVisual);
        }
    }
}
