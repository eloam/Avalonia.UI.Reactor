using Mvu.Primitives;
using Mvu.Views;

namespace Mvu.Theming;

public sealed record TextFieldTheme(
    ColorValue Background, ColorValue Foreground, ColorValue Placeholder,
    ColorValue BorderColor, ColorValue HoverBorderColor, ColorValue FocusBorderColor, ColorValue Caret,
    Thickness Padding, float Radius, float BorderWidth = 1, float FocusBorderWidth = 2)
{
    /// <summary>Largeur naturelle du champ (avant <c>.Grow()</c> / <c>.FillWidth()</c>).</summary>
    public float MinWidth { get; init; } = 160;
    public float CaretWidth { get; init; } = 1.5f;
    /// <summary>Demi-période du clignotement du curseur ; 0 = curseur fixe.</summary>
    public int CaretBlinkMs { get; init; } = 530;
    /// <summary>
    /// Apparence sur mesure de tous les champs de ce thème : le modèle dessine le cadre et place
    /// <c>state.Editor</c> là où le texte s'affiche. null = dessin standard.
    /// </summary>
    public Func<TextFieldState, View>? Template { get; init; }

    public static readonly TextFieldTheme Default = new(
        Background: ColorRole.Surface, Foreground: ColorRole.Text, Placeholder: ColorRole.TextMuted,
        BorderColor: ColorRole.FieldBorder, HoverBorderColor: ColorRole.TextMuted,
        FocusBorderColor: ColorRole.Accent, Caret: ColorRole.Text,
        Padding: new(10, 7), Radius: 6);
}
