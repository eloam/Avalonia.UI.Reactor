using Mvu.Primitives;

namespace Mvu.Rendering;

/// <summary>
/// Formes des indicateurs du dessin standard, dans un carré unité (de 0 à 1), mises à l'échelle de leur boîte.
/// Une autre forme passe par un modèle de contrôle (<c>Template</c>).
/// </summary>
internal static class Shapes
{
    /// <summary>Taille pour laquelle <c>CheckboxTheme.CheckThickness</c> est donnée ; le trait suit la taille de la case.</summary>
    public const float CheckboxReferenceSize = 18;

    /// <summary>Coche de la case à cocher (tracée à l'origine sur 18 px).</summary>
    public static readonly Point[] CheckboxMark =
        [new(4.5f / 18, 9.5f / 18), new(7.8f / 18, 12.8f / 18), new(13.5f / 18, 5.8f / 18)];

    /// <summary>Coche d'un élément de menu.</summary>
    public static readonly Point[] MenuCheckMark = [new(0.12f, 0.52f), new(0.40f, 0.80f), new(0.90f, 0.20f)];

    /// <summary>Flèche d'un élément qui ouvre un sous-menu.</summary>
    public static readonly Point[] SubmenuChevron = [new(0.30f, 0.10f), new(0.70f, 0.50f), new(0.30f, 0.90f)];

    /// <summary>La forme placée dans <paramref name="box"/>.</summary>
    public static Point[] Fit(Point[] shape, Rect box) =>
        [.. shape.Select(p => new Point(box.X + p.X * box.Width, box.Y + p.Y * box.Height))];
}
