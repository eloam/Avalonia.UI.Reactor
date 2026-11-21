// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Rendering;

/// <summary>
/// Defines what a <c>Canvas</c> can draw; the engine translates it into Avalonia drawing calls.
/// </summary>
/// <remarks>
/// Coordinates are in device-independent units; the engine applies the scale factor of the display.
/// </remarks>
public interface IDrawingContext
{
    /// <summary>
    /// Measures a line of text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font of the text.</param>
    /// <returns>The size of the text, in device-independent units.</returns>
    public Size MeasureText(string text, Font font);

    /// <summary>
    /// Fills a rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to fill.</param>
    /// <param name="color">The fill color.</param>
    /// <param name="radius">The corner radius.</param>
    public void FillRect(Rect rect, Color color, float radius = 0);

    /// <summary>
    /// Draws the outline of a rectangle, inside its bounds.
    /// </summary>
    /// <param name="rect">The rectangle to outline.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness.</param>
    /// <param name="radius">The corner radius.</param>
    public void StrokeRect(Rect rect, Color color, float thickness, float radius = 0);

    /// <summary>
    /// Draws a line with rounded ends.
    /// </summary>
    /// <param name="point0">The start point.</param>
    /// <param name="point1">The end point.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness.</param>
    public void DrawLine(Point point0, Point point1, Color color, float thickness);

    /// <summary>
    /// Draws a line of text, centered vertically in a rectangle.
    /// </summary>
    /// <param name="text">The text to draw.</param>
    /// <param name="bounds">The rectangle that contains the text.</param>
    /// <param name="font">The font of the text.</param>
    /// <param name="color">The text color.</param>
    /// <param name="align">The horizontal alignment of the text.</param>
    /// <param name="ellipsis">A value indicating whether text that does not fit ends with an ellipsis.</param>
    public void DrawText(string text, Rect bounds, Font font, Color color, TextAlign align = TextAlign.Start, bool ellipsis = true);

    /// <summary>
    /// Fills an ellipse.
    /// </summary>
    /// <param name="rect">The bounds of the ellipse.</param>
    /// <param name="color">The fill color.</param>
    public void FillEllipse(Rect rect, Color color);

    /// <summary>
    /// Draws the outline of an ellipse.
    /// </summary>
    /// <param name="rect">The bounds of the ellipse.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness.</param>
    public void StrokeEllipse(Rect rect, Color color, float thickness);

    /// <summary>
    /// Draws an arc of an ellipse with rounded ends.
    /// </summary>
    /// <param name="rect">The bounds of the ellipse.</param>
    /// <param name="startDegrees">The start angle, in degrees: 0 points right, -90 points up.</param>
    /// <param name="sweepDegrees">The angle covered by the arc, in degrees, clockwise.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness.</param>
    public void DrawArc(Rect rect, float startDegrees, float sweepDegrees, Color color, float thickness);

    /// <summary>
    /// Draws connected line segments with rounded joins.
    /// </summary>
    /// <param name="points">The points to connect, in order.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness.</param>
    /// <param name="closed">A value indicating whether the last point is connected to the first.</param>
    public void DrawPolyline(IReadOnlyList<Point> points, Color color, float thickness, bool closed = false);

    /// <summary>
    /// Fills a polygon.
    /// </summary>
    /// <param name="points">The vertices of the polygon.</param>
    /// <param name="color">The fill color.</param>
    public void FillPolygon(IReadOnlyList<Point> points, Color color);

    /// <summary>
    /// Draws the blurred shadow of a rounded rectangle, such as a card or a floating panel.
    /// </summary>
    /// <param name="rect">The rectangle that casts the shadow.</param>
    /// <param name="radius">The corner radius of the rectangle.</param>
    /// <param name="color">The shadow color.</param>
    /// <param name="blur">The blur radius.</param>
    /// <param name="offsetY">The vertical offset of the shadow.</param>
    public void DrawShadow(Rect rect, float radius, Color color, float blur, float offsetY);

    /// <summary>
    /// Restricts the following drawing to a rectangle, until the matching <see cref="PopClip"/>.
    /// </summary>
    /// <param name="rect">The clipping rectangle.</param>
    public void PushClip(Rect rect);

    /// <summary>
    /// Removes the clipping rectangle set by the last <see cref="PushClip"/>.
    /// </summary>
    public void PopClip();
}
