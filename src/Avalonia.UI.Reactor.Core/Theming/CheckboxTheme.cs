using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Theming;

public sealed record CheckboxTheme(
    ColorValue Background, ColorValue BorderColor, ColorValue HoverBorderColor,
    ColorValue CheckedBackground, ColorValue CheckMark,
    float Size = 18, float Radius = 4, float BorderWidth = 1.5f, float LabelSpacing = 8)
{
    /// <summary>Épaisseur du trait de la coche, pour une case de 18 (proportionnelle à <see cref="Size"/>).</summary>
    public float CheckThickness { get; init; } = 2;
    /// <summary>Voile au survol d'une case cochée.</summary>
    public float HoverOverlay { get; init; } = 0.1f;
    /// <summary>Apparence sur mesure de toutes les cases de ce thème ; null = dessin standard.</summary>
    public Func<CheckboxState, View>? Template { get; init; }

    public static readonly CheckboxTheme Default = new(
        Background: ColorRole.Surface, BorderColor: ColorRole.FieldBorder, HoverBorderColor: ColorRole.Accent,
        CheckedBackground: ColorRole.Accent, CheckMark: ColorRole.OnAccent);
}
