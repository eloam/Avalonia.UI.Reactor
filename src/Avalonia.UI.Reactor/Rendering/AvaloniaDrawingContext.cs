// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.UI.Reactor.Rendering;

using Avalonia.UI.Reactor.Reconciliation;

/// <summary>
/// Implements <see cref="IDrawingContext"/> on the drawing context of Avalonia, for <c>Canvas</c> views.
/// </summary>
/// <remarks>
/// Conventions: rectangle outlines are drawn inside their bounds, text is centered vertically and ends with an ellipsis,
/// lines and arcs have rounded ends. An instance lives for one rendering; disposing it removes the clips left in place.
/// </remarks>
/// <param name="context">The Avalonia drawing context.</param>
internal sealed class AvaloniaDrawingContext(DrawingContext context) : IDrawingContext, IDisposable
{
    // The largest sweep of an arc: a full turn would not differ from an empty arc.
    private const float MaxSweepDegrees = 359.99f;

    // An arc that covers more than a half turn takes the large-arc path.
    private const float HalfTurnDegrees = 180;

    private readonly Stack<IDisposable> _clips = new();

    /// <inheritdoc/>
    public Primitives.Size MeasureText(string text, Primitives.Font font)
    {
        FormattedText formatted = Format(text, font, Primitives.Color.Transparent);
        return new(text.Length == 0 ? 0 : (float)formatted.WidthIncludingTrailingWhitespace, (float)formatted.Height);
    }

    /// <inheritdoc/>
    public void FillRect(Primitives.Rect rect, Primitives.Color color, float radius = 0)
    {
        if (color.A == 0 || rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(Brush(color), null, ToRect(rect), radius, radius);
    }

    /// <inheritdoc/>
    public void StrokeRect(Primitives.Rect rect, Primitives.Color color, float thickness, float radius = 0)
    {
        if (color.A == 0)
        {
            return;
        }

        // The line is drawn inside the bounds.
        float half = thickness / 2;
        float innerRadius = Math.Max(0, radius - half);
        context.DrawRectangle(null, Pen(color, thickness), ToRect(rect).Deflate(half), innerRadius, innerRadius);
    }

    /// <inheritdoc/>
    public void DrawLine(Primitives.Point point0, Primitives.Point point1, Primitives.Color color, float thickness)
    {
        if (color.A == 0)
        {
            return;
        }

        context.DrawLine(Pen(color, thickness, PenLineCap.Round), ToPoint(point0), ToPoint(point1));
    }

    /// <inheritdoc/>
    public void DrawText(
        string text,
        Primitives.Rect bounds,
        Primitives.Font font,
        Primitives.Color color,
        Primitives.TextAlign align = Primitives.TextAlign.Start,
        bool ellipsis = true)
    {
        if (text.Length == 0 || color.A == 0)
        {
            return;
        }

        FormattedText formatted = Format(text, font, color);
        if (ellipsis)
        {
            formatted.MaxTextWidth = Math.Max(0, bounds.Width);
            formatted.MaxLineCount = 1;
            formatted.Trimming = TextTrimming.CharacterEllipsis;
        }

        context.DrawText(formatted, Origin(formatted, bounds, align));
    }

    /// <inheritdoc/>
    public void FillEllipse(Primitives.Rect rect, Primitives.Color color)
    {
        if (color.A == 0 || rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.DrawEllipse(Brush(color), null, ToRect(rect));
    }

    /// <inheritdoc/>
    public void StrokeEllipse(Primitives.Rect rect, Primitives.Color color, float thickness)
    {
        if (color.A == 0)
        {
            return;
        }

        context.DrawEllipse(null, Pen(color, thickness), ToRect(rect));
    }

    /// <inheritdoc/>
    public void DrawArc(Primitives.Rect rect, float startDegrees, float sweepDegrees, Primitives.Color color, float thickness)
    {
        if (color.A == 0 || sweepDegrees == 0)
        {
            return;
        }

        float sweep = Math.Clamp(sweepDegrees, -MaxSweepDegrees, MaxSweepDegrees);
        Rect bounds = ToRect(rect);
        var radius = new Size(bounds.Width / 2, bounds.Height / 2);

        Point PointAt(float degrees)
        {
            double angle = double.DegreesToRadians(degrees);
            return new(bounds.Center.X + radius.Width * Math.Cos(angle), bounds.Center.Y + radius.Height * Math.Sin(angle));
        }

        var geometry = new StreamGeometry();
        using (StreamGeometryContext figure = geometry.Open())
        {
            figure.BeginFigure(PointAt(startDegrees), isFilled: false);
            figure.ArcTo(
                PointAt(startDegrees + sweep),
                radius,
                rotationAngle: 0,
                isLargeArc: Math.Abs(sweep) > HalfTurnDegrees,
                sweep > 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
            figure.EndFigure(isClosed: false);
        }

        context.DrawGeometry(null, Pen(color, thickness, PenLineCap.Round), geometry);
    }

    /// <inheritdoc/>
    public void DrawPolyline(IReadOnlyList<Primitives.Point> points, Primitives.Color color, float thickness, bool closed = false)
    {
        if (color.A == 0 || points.Count < 2)
        {
            return;
        }

        context.DrawGeometry(null, Pen(color, thickness, PenLineCap.Round, PenLineJoin.Round), Path(points, closed, filled: false));
    }

    /// <inheritdoc/>
    public void FillPolygon(IReadOnlyList<Primitives.Point> points, Primitives.Color color)
    {
        if (color.A == 0 || points.Count < 3)
        {
            return;
        }

        context.DrawGeometry(Brush(color), null, Path(points, closed: true, filled: true));
    }

    /// <inheritdoc/>
    public void DrawShadow(Primitives.Rect rect, float radius, Primitives.Color color, float blur, float offsetY)
    {
        if (color.A == 0 || rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var shadow = new BoxShadow { OffsetY = offsetY, Blur = blur, Color = color.ToAvalonia() };
        context.DrawRectangle(null, null, new RoundedRect(ToRect(rect), radius), new BoxShadows(shadow));
    }

    /// <inheritdoc/>
    public void PushClip(Primitives.Rect rect) => _clips.Push(context.PushClip(ToRect(rect)));

    /// <inheritdoc/>
    public void PopClip() => _clips.Pop().Dispose();

    /// <summary>
    /// Removes the clips left in place by the drawing.
    /// </summary>
    public void Dispose()
    {
        while (_clips.Count > 0)
        {
            _clips.Pop().Dispose();
        }
    }

    private static FormattedText Format(string text, Primitives.Font font, Primitives.Color color) => new(
        text,
        CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        new Typeface(Conversions.FamilyNamed(font.Family), FontStyle.Normal, (FontWeight)font.Weight),
        font.Size,
        Brush(color));

    // The top-left corner of the text: aligned within the bounds, centered vertically.
    private static Point Origin(FormattedText formatted, Primitives.Rect bounds, Primitives.TextAlign align)
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

    private static StreamGeometry Path(IReadOnlyList<Primitives.Point> points, bool closed, bool filled)
    {
        var geometry = new StreamGeometry();
        using StreamGeometryContext figure = geometry.Open();
        figure.BeginFigure(ToPoint(points[0]), filled);
        for (int i = 1; i < points.Count; i++)
        {
            figure.LineTo(ToPoint(points[i]));
        }

        figure.EndFigure(closed);
        return geometry;
    }

    private static ImmutableSolidColorBrush Brush(Primitives.Color color) => new(color.ToAvalonia());

    private static Pen Pen(Primitives.Color color, float thickness, PenLineCap cap = PenLineCap.Flat, PenLineJoin join = PenLineJoin.Miter) =>
        new(Brush(color), thickness, lineCap: cap, lineJoin: join);

    private static Rect ToRect(Primitives.Rect rect) => new(rect.X, rect.Y, Math.Max(0, rect.Width), Math.Max(0, rect.Height));

    private static Point ToPoint(Primitives.Point point) => new(point.X, point.Y);
}
