using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Rendering;

/// <summary>
/// Formes des indicateurs du dessin standard, dans un carré unité (de 0 à 1), mises à l'échelle de leur boîte.
/// Une autre forme passe par un modèle de contrôle (<c>Template</c>).
/// </summary>
internal static class Shapes
{
    /// <summary>Taille pour laquelle <c>CheckboxTheme.CheckThickness</c> est donnée ; le trait suit la taille de la case.</summary>
    public const float CheckboxReferenceSize = 18;

    /// <summary>Coche de la case à cocher.</summary>
    public static readonly Point[] CheckboxMark =
        [new(4.5f / 18, 9.5f / 18), new(7.8f / 18, 12.8f / 18), new(13.5f / 18, 5.8f / 18)];

    /// <summary>La forme placée dans <paramref name="box"/>.</summary>
    public static Point[] Fit(Point[] shape, Rect box) =>
        [.. shape.Select(p => new Point(box.X + p.X * box.Width, box.Y + p.Y * box.Height))];
}
