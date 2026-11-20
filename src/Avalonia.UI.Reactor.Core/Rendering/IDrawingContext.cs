using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Rendering;

/// <summary>
/// Ce qu'un <c>Canvas</c> peut dessiner ; le moteur le traduit en appels de dessin d'Avalonia.
/// Toutes les coordonnées sont logiques (DIP) ; le backend applique le facteur DPI.
/// </summary>
public interface IDrawingContext
{
    /// <summary>Taille en unités logiques (DIP) d'une ligne de texte.</summary>
    Size MeasureText(string text, Font font);

    void FillRect(Rect r, Color color, float radius = 0);
    void StrokeRect(Rect r, Color color, float thickness, float radius = 0);
    void DrawLine(Point a, Point b, Color color, float thickness);
    void DrawText(string text, Rect bounds, Font font, Color color, TextAlign align = TextAlign.Start, bool ellipsis = true);
    void FillEllipse(Rect r, Color color);
    void StrokeEllipse(Rect r, Color color, float thickness);
    /// <summary>Arc (extrémités arrondies). Angles en degrés : 0 = à droite, sens horaire ; -90 = en haut.</summary>
    void DrawArc(Rect r, float startDegrees, float sweepDegrees, Color color, float thickness);
    void DrawPolyline(IReadOnlyList<Point> points, Color color, float thickness, bool closed = false);
    void FillPolygon(IReadOnlyList<Point> points, Color color);
    /// <summary>Ombre portée d'un rectangle arrondi (carte, fenêtre flottante), floutée de <paramref name="blur"/>.</summary>
    void DrawShadow(Rect r, float radius, Color color, float blur, float offsetY);
    void PushClip(Rect r);
    void PopClip();
}
