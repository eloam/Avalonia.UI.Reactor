using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.Rendering;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;

/// <summary>
/// <c>Canvas</c> : dessin libre. La fonction de dessin de la vue reçoit un <see cref="DrawScope"/> (thème, couleur et
/// police héritées) qui dessine sur le contexte d'Avalonia ; le dessin ne déborde pas des bornes du contrôle.
/// Le dessin a sa propre portée : un état lu pendant le dessin (<c>Canvas(d => … Progress …)</c>) le fait refaire quand
/// il change, sans réévaluer la vue qui contient le <c>Canvas</c>.
/// </summary>
internal sealed class DrawingCanvas : Control
{
    /// <summary>Nom de la portée du dessin (elle n'a pas de place dans l'arbre des portées).</summary>
    const string DrawingScopeId = "canvas";

    readonly Scope _drawing;
    Action<DrawScope>? _draw;
    Theme? _theme;
    Primitives.Color _foreground;
    Primitives.Font _font;
    int _redrawPending;

    public DrawingCanvas()
    {
        ClipToBounds = true;
        _drawing = new Scope(RequestRedraw, DrawingScopeId, identity: null);
    }

    /// <summary>Nouvelle version de la vue : son dessin est refait.</summary>
    public void Update(Action<DrawScope> draw, Theme theme, Primitives.Color foreground, Primitives.Font font)
    {
        _draw = draw;
        _theme = theme;
        _foreground = foreground;
        _font = font;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        Interlocked.Exchange(ref _redrawPending, 0);
        if (_draw is not { } draw || _theme is null) return;
        using var drawing = new AvaloniaDrawingContext(context);
        var scope = new DrawScope(drawing, new Primitives.Rect(0, 0, (float)Bounds.Width, (float)Bounds.Height), _theme, _foreground, _font);
        _drawing.Evaluate(() => draw(scope));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _drawing.Dispose(); // plus d'abonnement aux états tant que le contrôle n'est pas affiché
    }

    /// <summary>Un état lu par le dessin a changé (n'importe quel thread) : un seul redessin, sur le thread d'interface.</summary>
    void RequestRedraw()
    {
        if (Interlocked.Exchange(ref _redrawPending, 1) == 0) Dispatcher.UIThread.Post(InvalidateVisual);
    }
}
