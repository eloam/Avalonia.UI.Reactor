// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Rendering;

/// <summary>
/// Provides what a <c>Canvas</c> draws with: the area to draw, the current theme, and drawing helpers that accept theme
/// color roles (<c>ColorRole.Accent</c>) as well as fixed colors.
/// </summary>
/// <example>
/// <code>
/// Canvas(d =&gt; d.FillCircle(d.Bounds.Center, 8, ColorRole.Accent))
/// </code>
/// </example>
/// <param name="context">The drawing context of the canvas.</param>
/// <param name="bounds">The area to draw, in device-independent units.</param>
/// <param name="theme">The theme of the canvas branch.</param>
/// <param name="foreground">The inherited text color.</param>
/// <param name="font">The inherited font.</param>
public readonly struct DrawScope(IDrawingContext context, Rect bounds, Theme theme, Color foreground, Font font)
{
    /// <summary>
    /// Gets the drawing context, for what the helpers of this type do not cover (shadows, clipping).
    /// </summary>
    public IDrawingContext Context { get; } = context;

    /// <summary>
    /// Gets the area to draw, in device-independent units.
    /// </summary>
    public Rect Bounds { get; } = bounds;

    /// <summary>
    /// Gets the theme of the canvas branch.
    /// </summary>
    public Theme Theme { get; } = theme;

    /// <summary>
    /// Gets the inherited text color (the <c>.Foreground()</c> modifier, or the theme).
    /// </summary>
    public Color Foreground { get; } = foreground;

    /// <summary>
    /// Gets the inherited font.
    /// </summary>
    public Font Font { get; } = font;

    /// <summary>
    /// Resolves a color value against the palette of the theme.
    /// </summary>
    /// <param name="color">A fixed color, a color role or a named color.</param>
    /// <returns>The resolved color.</returns>
    public Color Color(ColorValue color) => color.Resolve(Theme.Palette);

    /// <summary>
    /// Fills a rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to fill.</param>
    /// <param name="color">The fill color.</param>
    /// <param name="radius">The corner radius.</param>
    public void FillRect(Rect rect, ColorValue color, float radius = 0) => Context.FillRect(rect, Color(color), radius);

    /// <summary>
    /// Draws the outline of a rectangle, inside its bounds.
    /// </summary>
    /// <param name="rect">The rectangle to outline.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness.</param>
    /// <param name="radius">The corner radius.</param>
    public void StrokeRect(Rect rect, ColorValue color, float thickness = 1, float radius = 0) =>
        Context.StrokeRect(rect, Color(color), thickness, radius);

    /// <summary>
    /// Draws a line with rounded ends.
    /// </summary>
    /// <param name="point0">The start point.</param>
    /// <param name="point1">The end point.</param>
    /// <param name="color">The line color; <see langword="null"/> for the inherited text color.</param>
    /// <param name="thickness">The line thickness.</param>
    public void Line(Point point0, Point point1, ColorValue? color = null, float thickness = 1) =>
        Context.DrawLine(point0, point1, ColorOrForeground(color), thickness);

    /// <summary>
    /// Fills a circle.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The fill color.</param>
    public void FillCircle(Point center, float radius, ColorValue color) =>
        Context.FillEllipse(Square(center, radius), Color(color));

    /// <summary>
    /// Draws the outline of a circle.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness.</param>
    public void StrokeCircle(Point center, float radius, ColorValue color, float thickness = 1) =>
        Context.StrokeEllipse(Square(center, radius), Color(color), thickness);

    /// <summary>
    /// Draws an arc of a circle with rounded ends.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="startDegrees">The start angle, in degrees: 0 points right, -90 points up.</param>
    /// <param name="sweepDegrees">The angle covered by the arc, in degrees, clockwise.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness.</param>
    public void Arc(Point center, float radius, float startDegrees, float sweepDegrees, ColorValue color, float thickness = 1) =>
        Context.DrawArc(Square(center, radius), startDegrees, sweepDegrees, Color(color), thickness);

    /// <summary>
    /// Draws connected line segments.
    /// </summary>
    /// <param name="points">The points to connect, in order.</param>
    /// <param name="color">The line color; <see langword="null"/> for the inherited text color.</param>
    /// <param name="thickness">The line thickness.</param>
    public void Polyline(IReadOnlyList<Point> points, ColorValue? color = null, float thickness = 1) =>
        Context.DrawPolyline(points, ColorOrForeground(color), thickness);

    /// <summary>
    /// Fills a polygon.
    /// </summary>
    /// <param name="points">The vertices of the polygon.</param>
    /// <param name="color">The fill color.</param>
    public void FillPolygon(IReadOnlyList<Point> points, ColorValue color) => Context.FillPolygon(points, Color(color));

    /// <summary>
    /// Draws a line of text with the inherited font, centered vertically in a rectangle and ending with an ellipsis if it
    /// does not fit.
    /// </summary>
    /// <param name="text">The text to draw.</param>
    /// <param name="rect">The rectangle that contains the text.</param>
    /// <param name="color">The text color; <see langword="null"/> for the inherited text color.</param>
    /// <param name="align">The horizontal alignment of the text.</param>
    public void Text(string text, Rect rect, ColorValue? color = null, TextAlign align = TextAlign.Start) =>
        Context.DrawText(text, rect, Font, ColorOrForeground(color), align);

    /// <summary>
    /// Measures a line of text with the inherited font.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>The size of the text, in device-independent units.</returns>
    public Size MeasureText(string text) => Context.MeasureText(text, Font);

    private Color ColorOrForeground(ColorValue? color) => color is { } value ? Color(value) : Foreground;

    private static Rect Square(Point center, float radius) =>
        new(center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
}
