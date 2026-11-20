using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Barre de menus dessinée dans la fenêtre : fond, bordure basse et marges. Les menus ouverts gardent pour l'instant
/// l'apparence d'Avalonia ; la barre du système (macOS) suit celle du système.
/// </summary>
public sealed record MenuTheme
{
    /// <summary>Fond de la barre.</summary>
    public ColorValue BarBackground { get; init; } = ColorRole.Surface;

    /// <summary>Couleur du trait qui sépare la barre du contenu.</summary>
    public ColorValue BarBorderColor { get; init; } = ColorRole.Border;

    /// <summary>Épaisseur de ce trait.</summary>
    public float BarBorderWidth { get; init; } = 1;

    /// <summary>Marges intérieures de la barre.</summary>
    public Thickness BarPadding { get; init; } = new(4, 2);

    /// <summary>Barre standard.</summary>
    public static readonly MenuTheme Default = new();
}
