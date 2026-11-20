namespace Avalonia.UI.Reactor.Theming;

/// <summary>Apparence d'une variante de bouton. Survol/appui = voile de la couleur de texte du thème
/// (assombrit en clair, éclaircit en sombre).</summary>
public sealed record ButtonVariant(
    ColorValue Background, ColorValue Foreground, ColorValue? BorderColor = null,
    float HoverOverlay = 0.08f, float PressedOverlay = 0.16f);
