using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Rendering;

/// <summary>Dessin standard d'une case à cocher, commun à tous les moteurs (le modèle d'un thème peut le remplacer).</summary>
internal static class CheckboxGlyph
{
    /// <summary>
    /// Case cochée : fond plein et coche, voilés au survol. Case vide : fond et bordure, bordure du survol au survol.
    /// La coche suit la taille de la case.
    /// </summary>
    public static void Draw(IDrawingContext ctx, Rect box, Theme theme, bool isChecked, bool hovered)
    {
        var style = theme.Checkbox;
        if (isChecked)
        {
            var background = theme[style.CheckedBackground];
            ctx.FillRect(box, hovered ? theme.Overlay(background, style.HoverOverlay) : background, style.Radius);
            var mark = Shapes.Fit(Shapes.CheckboxMark, box);
            float thickness = style.CheckThickness * box.Width / Shapes.CheckboxReferenceSize;
            ctx.DrawLine(mark[0], mark[1], theme[style.CheckMark], thickness);
            ctx.DrawLine(mark[1], mark[2], theme[style.CheckMark], thickness);
        }
        else
        {
            ctx.FillRect(box, theme[style.Background], style.Radius);
            ctx.StrokeRect(box, theme[hovered ? style.HoverBorderColor : style.BorderColor], style.BorderWidth, style.Radius);
        }
    }
}
