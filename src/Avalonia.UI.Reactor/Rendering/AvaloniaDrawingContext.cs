using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.UI.Reactor.Rendering;

using Avalonia.UI.Reactor.Reconciliation;

/// <summary>
/// <see cref="IDrawingContext"/> sur le contexte de dessin d'Avalonia, pour <c>Canvas</c>. Conventions : trait à l'intérieur des rectangles, texte centré verticalement et tronqué par « … »,
/// lignes et arcs aux extrémités arrondies. Vit le temps d'un rendu ; il retire à la fin les découpes laissées en place.
/// </summary>
internal sealed class AvaloniaDrawingContext(DrawingContext context) : IDrawingContext, IDisposable
{
    /// <summary>Le plus grand balayage d'un arc : un tour complet ne se distinguerait pas d'un arc vide.</summary>
    const float MaxSweepDegrees = 359.99f;

    readonly Stack<IDisposable> _clips = new();

    public Primitives.Size MeasureText(string text, Primitives.Font font)
    {
        var formatted = Format(text, font, Primitives.Color.Transparent);
        return new(text.Length == 0 ? 0 : (float)formatted.WidthIncludingTrailingWhitespace, (float)formatted.Height);
    }

    public void FillRect(Primitives.Rect r, Primitives.Color color, float radius = 0)
    {
        if (color.A == 0 || r.Width <= 0 || r.Height <= 0) return;
        context.DrawRectangle(Brush(color), null, ToRect(r), radius, radius);
    }

    public void StrokeRect(Primitives.Rect r, Primitives.Color color, float thickness, float radius = 0)
    {
        if (color.A == 0) return;
        float half = thickness / 2; // trait à l'intérieur des bornes
        context.DrawRectangle(null, Pen(color, thickness), ToRect(r).Deflate(half), Math.Max(0, radius - half), Math.Max(0, radius - half));
    }

    public void DrawLine(Primitives.Point a, Primitives.Point b, Primitives.Color color, float thickness)
    {
        if (color.A == 0) return;
        context.DrawLine(Pen(color, thickness, PenLineCap.Round), ToPoint(a), ToPoint(b));
    }

    public void DrawText(string text, Primitives.Rect bounds, Primitives.Font font, Primitives.Color color,
        Primitives.TextAlign align = Primitives.TextAlign.Start, bool ellipsis = true)
    {
        if (text.Length == 0 || color.A == 0) return;
        var formatted = Format(text, font, color);
        if (ellipsis)
        {
            formatted.MaxTextWidth = Math.Max(0, bounds.Width);
            formatted.MaxLineCount = 1;
            formatted.Trimming = TextTrimming.CharacterEllipsis;
        }
        context.DrawText(formatted, Origin(formatted, bounds, align));
    }

    public void FillEllipse(Primitives.Rect r, Primitives.Color color)
    {
        if (color.A == 0 || r.Width <= 0 || r.Height <= 0) return;
        context.DrawEllipse(Brush(color), null, ToRect(r));
    }

    public void StrokeEllipse(Primitives.Rect r, Primitives.Color color, float thickness)
    {
        if (color.A == 0) return;
        context.DrawEllipse(null, Pen(color, thickness), ToRect(r));
    }

    public void DrawArc(Primitives.Rect r, float startDegrees, float sweepDegrees, Primitives.Color color, float thickness)
    {
        if (color.A == 0 || sweepDegrees == 0) return;
        float sweep = Math.Clamp(sweepDegrees, -MaxSweepDegrees, MaxSweepDegrees);
        var bounds = ToRect(r);
        var radius = new Size(bounds.Width / 2, bounds.Height / 2);

        Point At(float degrees)
        {
            double angle = degrees * Math.PI / 180;
            return new(bounds.Center.X + radius.Width * Math.Cos(angle), bounds.Center.Y + radius.Height * Math.Sin(angle));
        }

        var geometry = new StreamGeometry();
        using (var figure = geometry.Open())
        {
            figure.BeginFigure(At(startDegrees), isFilled: false);
            figure.ArcTo(At(startDegrees + sweep), radius, 0, Math.Abs(sweep) > 180,
                sweep > 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
            figure.EndFigure(isClosed: false);
        }
        context.DrawGeometry(null, Pen(color, thickness, PenLineCap.Round), geometry);
    }

    public void DrawPolyline(IReadOnlyList<Primitives.Point> points, Primitives.Color color, float thickness, bool closed = false)
    {
        if (color.A == 0 || points.Count < 2) return;
        context.DrawGeometry(null, Pen(color, thickness, PenLineCap.Round, PenLineJoin.Round), Path(points, closed, filled: false));
    }

    public void FillPolygon(IReadOnlyList<Primitives.Point> points, Primitives.Color color)
    {
        if (color.A == 0 || points.Count < 3) return;
        context.DrawGeometry(Brush(color), null, Path(points, closed: true, filled: true));
    }

    public void DrawShadow(Primitives.Rect r, float radius, Primitives.Color color, float blur, float offsetY)
    {
        if (color.A == 0 || r.Width <= 0 || r.Height <= 0) return;
        var shadow = new BoxShadow { OffsetY = offsetY, Blur = blur, Color = color.ToAvalonia() };
        context.DrawRectangle(null, null, new RoundedRect(ToRect(r), radius), new BoxShadows(shadow));
    }

    public void PushClip(Primitives.Rect r) => _clips.Push(context.PushClip(ToRect(r)));

    public void PopClip() => _clips.Pop().Dispose();

    public void Dispose()
    {
        while (_clips.Count > 0) _clips.Pop().Dispose();
    }

    static FormattedText Format(string text, Primitives.Font font, Primitives.Color color) => new(
        text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
        new Typeface(Conversions.FamilyNamed(font.Family), FontStyle.Normal, (FontWeight)font.Weight),
        font.Size, Brush(color));

    /// <summary>Coin haut gauche du texte : aligné dans les bornes, centré verticalement.</summary>
    static Point Origin(FormattedText formatted, Primitives.Rect bounds, Primitives.TextAlign align)
    {
        double width = formatted.WidthIncludingTrailingWhitespace;
        double x = align switch
        {
            Primitives.TextAlign.Center => bounds.X + (bounds.Width - width) / 2,
            Primitives.TextAlign.End => bounds.Right - width,
            _ => bounds.X,
        };
        return new(x, bounds.Y + (bounds.Height - formatted.Height) / 2);
    }

    static StreamGeometry Path(IReadOnlyList<Primitives.Point> points, bool closed, bool filled)
    {
        var geometry = new StreamGeometry();
        using var figure = geometry.Open();
        figure.BeginFigure(ToPoint(points[0]), filled);
        for (int i = 1; i < points.Count; i++) figure.LineTo(ToPoint(points[i]));
        figure.EndFigure(closed);
        return geometry;
    }

    static ImmutableSolidColorBrush Brush(Primitives.Color color) => new(color.ToAvalonia());

    static Pen Pen(Primitives.Color color, float thickness, PenLineCap cap = PenLineCap.Flat, PenLineJoin join = PenLineJoin.Miter) =>
        new(Brush(color), thickness, lineCap: cap, lineJoin: join);

    static Rect ToRect(Primitives.Rect r) => new(r.X, r.Y, Math.Max(0, r.Width), Math.Max(0, r.Height));

    static Point ToPoint(Primitives.Point p) => new(p.X, p.Y);
}
