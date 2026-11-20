using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Panels;

/// <summary>
/// Souplesse d'un enfant dans son conteneur (<see cref="FlexStack"/>, <see cref="LayerStack"/>), posée par le
/// réconciliateur à partir de la vue : part de l'espace restant, remplissage, alignement propre.
/// </summary>
internal sealed class Flex
{
    /// <summary>Part de l'espace restant donnée à un enfant (<c>.Grow()</c>, 1 pour <c>Spacer()</c>).</summary>
    public static readonly AttachedProperty<double> GrowProperty =
        AvaloniaProperty.RegisterAttached<Flex, Control, double>("Grow");

    /// <summary>L'enfant prend toute la largeur disponible.</summary>
    public static readonly AttachedProperty<bool> FillWidthProperty =
        AvaloniaProperty.RegisterAttached<Flex, Control, bool>("FillWidth");

    /// <summary>L'enfant prend toute la hauteur disponible.</summary>
    public static readonly AttachedProperty<bool> FillHeightProperty =
        AvaloniaProperty.RegisterAttached<Flex, Control, bool>("FillHeight");

    /// <summary>Alignement propre à un enfant (<c>.AlignSelf(…)</c>) ; null = celui du conteneur.</summary>
    public static readonly AttachedProperty<Primitives.Align?> SelfAlignProperty =
        AvaloniaProperty.RegisterAttached<Flex, Control, Primitives.Align?>("SelfAlign");

    Flex() { }

    /// <summary>Propriétés qui changent la place d'un enfant : un conteneur les déclare pour se remettre en page.</summary>
    public static AvaloniaProperty[] ChildProperties => [GrowProperty, FillWidthProperty, FillHeightProperty, SelfAlignProperty];

    public static double GetGrow(Control child) => child.GetValue(GrowProperty);
    public static void SetGrow(Control child, double value) => child.SetValue(GrowProperty, value);
    public static bool GetFillWidth(Control child) => child.GetValue(FillWidthProperty);
    public static void SetFillWidth(Control child, bool value) => child.SetValue(FillWidthProperty, value);
    public static bool GetFillHeight(Control child) => child.GetValue(FillHeightProperty);
    public static void SetFillHeight(Control child, bool value) => child.SetValue(FillHeightProperty, value);
    public static Primitives.Align? GetSelfAlign(Control child) => child.GetValue(SelfAlignProperty);
    public static void SetSelfAlign(Control child, Primitives.Align? value) => child.SetValue(SelfAlignProperty, value);

    /// <summary>
    /// Place un enfant sur un axe : étiré s'il remplit ou si l'alignement le demande, sinon à sa taille naturelle
    /// (sans dépasser), au début, au centre ou à la fin de la place offerte.
    /// </summary>
    public static (double Position, double Size) Place(double available, double natural, bool fill, Primitives.Align align)
    {
        double size = fill || align == Primitives.Align.Stretch ? available : Math.Min(natural, available);
        double position = align switch
        {
            Primitives.Align.Center => (available - size) / 2,
            Primitives.Align.End => available - size,
            _ => 0,
        };
        return (position, size);
    }
}
