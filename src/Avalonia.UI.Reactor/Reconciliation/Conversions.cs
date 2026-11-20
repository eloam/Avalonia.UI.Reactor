using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.UI.Reactor.Reconciliation;

/// <summary>Conversion de nos valeurs (couleurs, marges, polices) vers celles d'Avalonia.</summary>
internal static class Conversions
{
    static readonly Dictionary<string, FontFamily> Families = [];

    public static Color ToAvalonia(this Primitives.Color c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    public static Thickness ToAvalonia(this Primitives.Thickness t) => new(t.Left, t.Top, t.Right, t.Bottom);

    /// <summary>
    /// Pose une couleur unie sur une propriété de pinceau, sans remplacer un pinceau de la même couleur : un nouveau
    /// pinceau à chaque mise à jour redessinerait le contrôle pour rien. null rend la propriété à sa valeur par défaut.
    /// </summary>
    public static void SetBrush(this AvaloniaObject target, StyledProperty<IBrush?> property, Primitives.Color? color)
    {
        if (color is not { } c)
        {
            target.ClearValue(property);
            return;
        }
        var value = c.ToAvalonia();
        if (target.GetValue(property) is ISolidColorBrush current && current.Color == value && current.Opacity == 1) return;
        target.SetValue(property, new ImmutableSolidColorBrush(value));
    }

    /// <summary>Police d'un texte ou d'un contrôle (propriétés héritées de <see cref="TextElement"/>).</summary>
    public static void SetFont(this AvaloniaObject target, Primitives.Font font)
    {
        target.SetValue(TextElement.FontFamilyProperty, FamilyNamed(font.Family));
        target.SetValue(TextElement.FontSizeProperty, font.Size);
        target.SetValue(TextElement.FontWeightProperty, (FontWeight)font.Weight);
    }

    /// <summary>Une seule instance par nom de police (thread d'interface uniquement).</summary>
    internal static FontFamily FamilyNamed(string name)
    {
        if (!Families.TryGetValue(name, out var family)) Families[name] = family = new FontFamily(name);
        return family;
    }
}
