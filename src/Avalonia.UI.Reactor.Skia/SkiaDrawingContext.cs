using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Rendering;
using SkiaSharp;

namespace Avalonia.UI.Reactor.Skia;

/// <summary>
/// Implémentation de <see cref="IDrawingContext"/> sur Skia. Indépendante de la fenêtre :
/// elle dessine sur n'importe quel <see cref="SKCanvas"/> (GPU OpenGL/Vulkan/Metal/D3D, ou CPU).
/// Le canvas est mis à l'échelle DPI une fois : tout le reste travaille en unités logiques.
/// </summary>
public sealed class SkiaDrawingContext : IDrawingContext, IDisposable
{
    /// <summary>Écart de graisse à partir duquel la police trouvée est trop maigre : le gras est alors simulé.</summary>
    const int SyntheticBoldMinimumGap = 150;

    readonly Dictionary<(string, int), SKTypeface> _typefaces = [];
    readonly Dictionary<Font, SKFont> _fonts = [];
    readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    SKCanvas? _canvas;
    int _fontsVersion = -1;

    /// <summary>Police de l'application si elle est enregistrée (<see cref="Fonts"/>), sinon celle du système.</summary>
    SKTypeface TypefaceFor(string family, int weight)
    {
        if (!_typefaces.TryGetValue((family, weight), out var tf))
        {
            tf = Fonts.Find(family, weight) is { } data
                ? SKTypeface.FromData(SKData.CreateCopy(data)) ?? SKTypeface.Default
                : SKTypeface.FromFamilyName(family, weight, (int)SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                  ?? SKTypeface.Default;
            _typefaces[(family, weight)] = tf;
        }
        return tf;
    }

    /// <summary>Une police a été enregistrée depuis le dernier rendu : on oublie les polices déjà chargées.</summary>
    void SyncFonts()
    {
        if (_fontsVersion == Fonts.Version) return;
        _fontsVersion = Fonts.Version;
        foreach (var f in _fonts.Values) f.Dispose();
        _fonts.Clear();
        _typefaces.Clear();
    }

    public void Begin(SKCanvas canvas, float scale)
    {
        _canvas = canvas;
        canvas.ResetMatrix();
        canvas.Scale(scale);
    }

    SKCanvas Canvas => _canvas ?? throw new InvalidOperationException("Call 'Begin' before drawing.");

    SKFont FontFor(Font f)
    {
        SyncFonts();
        if (!_fonts.TryGetValue(f, out var font))
        {
            var typeface = TypefaceFor(f.Family, f.Weight);
            font = new SKFont(typeface, f.Size)
            {
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias,
                // Graisse demandée absente de la police (ex. seule la version normale est fournie) : gras simulé.
                Embolden = f.Weight >= FontWeight.SemiBold && typeface.FontWeight < f.Weight - SyntheticBoldMinimumGap,
            };
            _fonts[f] = font;
        }
        return font;
    }

    static SKColor C(Color c) => new(c.R, c.G, c.B, c.A);
    static SKRect R(Rect r) => new(r.X, r.Y, r.Right, r.Bottom);

    public Size MeasureText(string text, Font font)
    {
        var f = FontFor(font);
        var m = f.Metrics;
        return new Size(text.Length == 0 ? 0 : f.MeasureText(text), m.Descent - m.Ascent);
    }

    public void FillRect(Rect r, Color color, float radius = 0)
    {
        if (color.A == 0 || r.Width <= 0 || r.Height <= 0) return;
        _fill.Color = C(color);
        if (radius > 0) Canvas.DrawRoundRect(R(r), radius, radius, _fill);
        else Canvas.DrawRect(R(r), _fill);
    }

    public void StrokeRect(Rect r, Color color, float thickness, float radius = 0)
    {
        if (color.A == 0) return;
        _stroke.Color = C(color);
        _stroke.StrokeWidth = thickness;
        float h = thickness / 2; // trait à l'intérieur des bornes
        var rr = new SKRect(r.X + h, r.Y + h, r.Right - h, r.Bottom - h);
        Canvas.DrawRoundRect(rr, Math.Max(0, radius - h), Math.Max(0, radius - h), _stroke);
    }

    public void DrawLine(Point a, Point b, Color color, float thickness)
    {
        _stroke.Color = C(color);
        _stroke.StrokeWidth = thickness;
        _stroke.StrokeCap = SKStrokeCap.Round;
        Canvas.DrawLine(a.X, a.Y, b.X, b.Y, _stroke);
        _stroke.StrokeCap = SKStrokeCap.Butt;
    }

    public void DrawText(string text, Rect bounds, Font font, Color color, TextAlign align = TextAlign.Start, bool ellipsis = true)
    {
        if (text.Length == 0 || color.A == 0) return;
        var f = FontFor(font);
        float width = f.MeasureText(text);
        if (ellipsis && width > bounds.Width + 0.5f)
        {
            text = Ellipsize(text, f, bounds.Width);
            width = f.MeasureText(text);
        }
        var m = f.Metrics;
        float x = align switch
        {
            TextAlign.Center => bounds.X + (bounds.Width - width) / 2,
            TextAlign.End => bounds.Right - width,
            _ => bounds.X,
        };
        float y = bounds.Y + (bounds.Height - (m.Descent - m.Ascent)) / 2 - m.Ascent;
        _fill.Color = C(color);
        Canvas.DrawText(text, x, y, SKTextAlign.Left, f, _fill);
    }

    static string Ellipsize(string text, SKFont f, float max)
    {
        int lo = 0, hi = text.Length;
        while (lo < hi) // recherche dichotomique du plus long préfixe qui tient
        {
            int mid = (lo + hi + 1) / 2;
            if (f.MeasureText(string.Concat(text.AsSpan(0, mid), "…")) <= max) lo = mid; else hi = mid - 1;
        }
        return string.Concat(text.AsSpan(0, lo).TrimEnd(), "…");
    }

    public void FillEllipse(Rect r, Color color)
    {
        if (color.A == 0 || r.Width <= 0 || r.Height <= 0) return;
        _fill.Color = C(color);
        Canvas.DrawOval(R(r), _fill);
    }

    public void StrokeEllipse(Rect r, Color color, float thickness)
    {
        if (color.A == 0) return;
        _stroke.Color = C(color);
        _stroke.StrokeWidth = thickness;
        Canvas.DrawOval(R(r), _stroke);
    }

    public void DrawArc(Rect r, float startDegrees, float sweepDegrees, Color color, float thickness)
    {
        if (color.A == 0 || sweepDegrees == 0) return;
        _stroke.Color = C(color);
        _stroke.StrokeWidth = thickness;
        _stroke.StrokeCap = SKStrokeCap.Round;
        using var builder = new SKPathBuilder();
        builder.AddArc(R(r), startDegrees, Math.Clamp(sweepDegrees, -359.99f, 359.99f));
        using var path = builder.Detach();
        Canvas.DrawPath(path, _stroke);
        _stroke.StrokeCap = SKStrokeCap.Butt;
    }

    public void DrawPolyline(IReadOnlyList<Point> points, Color color, float thickness, bool closed = false)
    {
        if (color.A == 0 || points.Count < 2) return;
        _stroke.Color = C(color);
        _stroke.StrokeWidth = thickness;
        _stroke.StrokeCap = SKStrokeCap.Round;
        _stroke.StrokeJoin = SKStrokeJoin.Round;
        using var path = Path(points, closed);
        Canvas.DrawPath(path, _stroke);
        _stroke.StrokeCap = SKStrokeCap.Butt;
        _stroke.StrokeJoin = SKStrokeJoin.Miter;
    }

    public void FillPolygon(IReadOnlyList<Point> points, Color color)
    {
        if (color.A == 0 || points.Count < 3) return;
        _fill.Color = C(color);
        using var path = Path(points, closed: true);
        Canvas.DrawPath(path, _fill);
    }

    static SKPath Path(IReadOnlyList<Point> points, bool closed)
    {
        using var builder = new SKPathBuilder();
        builder.MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Count; i++) builder.LineTo(points[i].X, points[i].Y);
        if (closed) builder.Close();
        return builder.Detach();
    }

    public void UnderlineText(string text, int start, int length, Rect bounds, Font font, Color color, TextAlign align = TextAlign.Start)
    {
        if (color.A == 0 || start < 0 || length <= 0 || start + length > text.Length) return;
        var f = FontFor(font);
        var m = f.Metrics;
        float width = f.MeasureText(text);
        float x = align switch
        {
            TextAlign.Center => bounds.X + (bounds.Width - width) / 2,
            TextAlign.End => bounds.Right - width,
            _ => bounds.X,
        };
        float baseline = bounds.Y + (bounds.Height - (m.Descent - m.Ascent)) / 2 - m.Ascent;
        float thickness = m.UnderlineThickness is float t && t > 0 ? t : Math.Max(1, font.Size / 14);
        float offset = m.UnderlinePosition is float p && p > 0 ? p : thickness;
        float x0 = x + f.MeasureText(text.AsSpan(0, start));
        float x1 = x0 + f.MeasureText(text.AsSpan(start, length));
        _fill.Color = C(color);
        Canvas.DrawRect(x0, baseline + offset, x1 - x0, thickness, _fill);
    }

    public void DrawShadow(Rect r, float radius, Color color, float blur, float offsetY)
    {
        if (color.A == 0 || r.Width <= 0 || r.Height <= 0) return;
        using var blurFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, Math.Max(0.1f, blur / 2));
        using var paint = new SKPaint { IsAntialias = true, Color = C(color), MaskFilter = blurFilter };
        Canvas.DrawRoundRect(new SKRect(r.X, r.Y + offsetY, r.Right, r.Bottom + offsetY), radius, radius, paint);
    }

    public void PushClip(Rect r)
    {
        Canvas.Save();
        Canvas.ClipRect(R(r), SKClipOperation.Intersect, true);
    }

    public void PopClip() => Canvas.Restore();

    public void Dispose()
    {
        foreach (var f in _fonts.Values) f.Dispose();
        _fill.Dispose();
        _stroke.Dispose();
        foreach (var t in _typefaces.Values) t.Dispose();
    }
}
