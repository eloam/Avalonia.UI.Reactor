using Mvu.Primitives;

namespace Mvu.Rendering;

/// <summary>
/// Seul contrat qu'un backend (Skia, Direct2D, …) doit implémenter pour dessiner.
/// Toutes les coordonnées sont logiques (DIP) ; le backend applique le facteur DPI.
/// </summary>
public interface IDrawingContext : ITextMeasurer
{
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
    /// <summary>Ombre portée d'un rectangle arrondi (menus, fenêtres flottantes), floutée de <paramref name="blur"/>.</summary>
    void DrawShadow(Rect r, float radius, Color color, float blur, float offsetY);
    /// <summary>
    /// Souligne les caractères <paramref name="start"/> à <paramref name="start"/> + <paramref name="length"/> d'un texte
    /// dessiné par <see cref="DrawText"/> avec les mêmes bornes, police et alignement (lettres d'accès des menus).
    /// </summary>
    void UnderlineText(string text, int start, int length, Rect bounds, Font font, Color color, TextAlign align = TextAlign.Start);
    void PushClip(Rect r);
    void PopClip();
}
