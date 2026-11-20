using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Rendering;

/// <summary>
/// Ce que reçoit un <c>Canvas</c> : la zone à dessiner, le thème actif, et des raccourcis de dessin
/// qui acceptent des rôles de couleur (<c>ColorRole.Accent</c>) comme des couleurs fixes.
/// <code>Canvas(d => d.FillCircle(d.Bounds.Center, 8, ColorRole.Accent))</code>
/// </summary>
public readonly struct DrawScope
{
    public DrawScope(IDrawingContext context, Rect bounds, Theme theme, Color foreground, Font font)
    {
        Context = context;
        Bounds = bounds;
        Theme = theme;
        Foreground = foreground;
        Font = font;
    }

    /// <summary>Accès direct au backend, pour ce que les raccourcis ne couvrent pas.</summary>
    public IDrawingContext Context { get; }
    public Rect Bounds { get; }
    public Theme Theme { get; }
    /// <summary>Couleur de texte héritée (modificateur <c>.Foreground()</c> ou thème).</summary>
    public Color Foreground { get; }
    public Font Font { get; }

    public Color Color(ColorValue c) => c.Resolve(Theme.Palette);
    Color Or(ColorValue? c) => c is { } v ? Color(v) : Foreground;

    public void FillRect(Rect r, ColorValue color, float radius = 0) => Context.FillRect(r, Color(color), radius);
    public void StrokeRect(Rect r, ColorValue color, float thickness = 1, float radius = 0) =>
        Context.StrokeRect(r, Color(color), thickness, radius);
    public void Line(Point a, Point b, ColorValue? color = null, float thickness = 1) => Context.DrawLine(a, b, Or(color), thickness);
    public void FillCircle(Point center, float radius, ColorValue color) =>
        Context.FillEllipse(new Rect(center.X - radius, center.Y - radius, 2 * radius, 2 * radius), Color(color));
    public void StrokeCircle(Point center, float radius, ColorValue color, float thickness = 1) =>
        Context.StrokeEllipse(new Rect(center.X - radius, center.Y - radius, 2 * radius, 2 * radius), Color(color), thickness);
    public void Arc(Point center, float radius, float startDegrees, float sweepDegrees, ColorValue color, float thickness = 1) =>
        Context.DrawArc(new Rect(center.X - radius, center.Y - radius, 2 * radius, 2 * radius), startDegrees, sweepDegrees, Color(color), thickness);
    public void Polyline(IReadOnlyList<Point> points, ColorValue? color = null, float thickness = 1) =>
        Context.DrawPolyline(points, Or(color), thickness);
    public void FillPolygon(IReadOnlyList<Point> points, ColorValue color) => Context.FillPolygon(points, Color(color));
    public void Text(string text, Rect r, ColorValue? color = null, TextAlign align = TextAlign.Start) =>
        Context.DrawText(text, r, Font, Or(color), align);
    public Size MeasureText(string text) => Context.MeasureText(text, Font);
}
