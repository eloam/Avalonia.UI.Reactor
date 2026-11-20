using Mvu.Primitives;
using Mvu.Views;

namespace Mvu.Theming;

/// <summary>
/// Barre de menus dessinée et menus ouverts. Survol et sélection utilisent par défaut un voile de la couleur
/// du texte, comme les boutons discrets ; tout peut être remplacé, jusqu'à l'apparence complète
/// d'un titre ou d'un élément (<see cref="TitleTemplate"/>, <see cref="ItemTemplate"/>).
/// </summary>
public sealed record MenuTheme
{
    // Barre
    public ColorValue BarBackground { get; init; } = ColorRole.Surface;
    public ColorValue BarBorderColor { get; init; } = ColorRole.Border;
    public float BarBorderWidth { get; init; } = 1;
    public Thickness BarPadding { get; init; } = new(4, 2);
    public Thickness TitlePadding { get; init; } = new(10, 5);
    public float TitleRadius { get; init; } = 4;

    // Menu ouvert
    public ColorValue Background { get; init; } = ColorRole.Surface;
    public ColorValue BorderColor { get; init; } = ColorRole.Border;
    public float Radius { get; init; } = 6;
    public Thickness Padding { get; init; } = new(4);
    public float MinWidth { get; init; } = 200;
    public Color ShadowColor { get; init; } = new(0, 0, 0, 56);
    public float ShadowBlur { get; init; } = 14;
    public float ShadowOffsetY { get; init; } = 4;

    // Éléments
    public Thickness ItemPadding { get; init; } = new(8, 5);
    public float ItemRadius { get; init; } = 4;
    /// <summary>Largeur réservée à la coche, devant les libellés.</summary>
    public float CheckColumnWidth { get; init; } = 22;
    /// <summary>Écart minimal entre un libellé et son raccourci.</summary>
    public float ShortcutSpacing { get; init; } = 32;
    public ColorValue ShortcutForeground { get; init; } = ColorRole.TextMuted;
    /// <summary>Fond d'un élément surligné ou d'un titre ouvert ; null = voile de la couleur du texte.</summary>
    public ColorValue? HighlightBackground { get; init; }
    public ColorValue? HighlightForeground { get; init; }
    public float HighlightOverlay { get; init; } = 0.08f;
    /// <summary>Opacité d'un élément désactivé.</summary>
    public float DisabledOpacity { get; init; } = 0.45f;
    public float SeparatorSpacing { get; init; } = 4;
    /// <summary>Taille de la coche et de la flèche des sous-menus ; épaisseur de leur trait.</summary>
    public float IndicatorSize { get; init; } = 10;
    public float IndicatorThickness { get; init; } = 1.5f;

    /// <summary>Apparence sur mesure des titres de la barre ; null = dessin standard.</summary>
    public Func<MenuTitleState, View>? TitleTemplate { get; init; }
    /// <summary>Apparence sur mesure des éléments de menu ; null = dessin standard.</summary>
    public Func<MenuItemState, View>? ItemTemplate { get; init; }

    public static readonly MenuTheme Default = new();
}
