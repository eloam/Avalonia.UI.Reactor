using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Panels;

/// <summary>
/// Pile de <c>VStack</c> / <c>HStack</c>, avec les règles de SwiftUI : chaque enfant prend sa taille naturelle sur l'axe
/// de la pile ; la place restante va aux enfants qui s'étirent (<c>Spacer()</c>, <c>.Grow()</c>, <c>.FillWidth()</c>) au
/// prorata de leur part. Sur l'autre axe, un enfant est aligné (début, centre, fin) ou étiré.
/// Fond, bordure, angles et marge intérieure sont portés par l'enveloppe que le réconciliateur pose autour.
/// </summary>
internal sealed class FlexStack : Panel
{
    public static readonly StyledProperty<Primitives.Axis> AxisProperty =
        AvaloniaProperty.Register<FlexStack, Primitives.Axis>(nameof(Axis), Primitives.Axis.Vertical);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<FlexStack, double>(nameof(Spacing));

    public static readonly StyledProperty<Primitives.Align> CrossAlignProperty =
        AvaloniaProperty.Register<FlexStack, Primitives.Align>(nameof(CrossAlign));

    /// <summary>Part de l'espace restant donnée à un enfant (<c>.Grow()</c>, 1 pour <c>Spacer()</c>).</summary>
    public static readonly AttachedProperty<double> GrowProperty =
        AvaloniaProperty.RegisterAttached<FlexStack, Control, double>("Grow");

    /// <summary>L'enfant prend toute la largeur disponible.</summary>
    public static readonly AttachedProperty<bool> FillWidthProperty =
        AvaloniaProperty.RegisterAttached<FlexStack, Control, bool>("FillWidth");

    /// <summary>L'enfant prend toute la hauteur disponible.</summary>
    public static readonly AttachedProperty<bool> FillHeightProperty =
        AvaloniaProperty.RegisterAttached<FlexStack, Control, bool>("FillHeight");

    /// <summary>Alignement propre à un enfant sur l'axe transverse (<c>.AlignSelf(…)</c>) ; null = celui de la pile.</summary>
    public static readonly AttachedProperty<Primitives.Align?> SelfAlignProperty =
        AvaloniaProperty.RegisterAttached<FlexStack, Control, Primitives.Align?>("SelfAlign");

    static FlexStack()
    {
        AffectsMeasure<FlexStack>(AxisProperty, SpacingProperty);
        AffectsArrange<FlexStack>(CrossAlignProperty);
        AffectsParentMeasure<FlexStack>(GrowProperty, FillWidthProperty, FillHeightProperty, SelfAlignProperty);
    }

    public Primitives.Axis Axis { get => GetValue(AxisProperty); set => SetValue(AxisProperty, value); }
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public Primitives.Align CrossAlign { get => GetValue(CrossAlignProperty); set => SetValue(CrossAlignProperty, value); }

    public static double GetGrow(Control child) => child.GetValue(GrowProperty);
    public static void SetGrow(Control child, double value) => child.SetValue(GrowProperty, value);
    public static bool GetFillWidth(Control child) => child.GetValue(FillWidthProperty);
    public static void SetFillWidth(Control child, bool value) => child.SetValue(FillWidthProperty, value);
    public static bool GetFillHeight(Control child) => child.GetValue(FillHeightProperty);
    public static void SetFillHeight(Control child, bool value) => child.SetValue(FillHeightProperty, value);
    public static Primitives.Align? GetSelfAlign(Control child) => child.GetValue(SelfAlignProperty);
    public static void SetSelfAlign(Control child, Primitives.Align? value) => child.SetValue(SelfAlignProperty, value);

    bool Vertical => Axis == Primitives.Axis.Vertical;

    /// <summary>Taille naturelle : la somme des enfants sur l'axe de la pile (plus l'espacement), le plus grand sur l'autre.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        // Chaque enfant donne sa taille naturelle sur l'axe de la pile ; sur l'autre, il connaît la place offerte.
        var offered = Vertical
            ? new Size(availableSize.Width, double.PositiveInfinity)
            : new Size(double.PositiveInfinity, availableSize.Height);

        double main = 0, cross = 0;
        foreach (var child in Children)
        {
            child.Measure(offered);
            var desired = child.DesiredSize;
            main += Vertical ? desired.Height : desired.Width;
            cross = Math.Max(cross, Vertical ? desired.Width : desired.Height);
        }
        if (Children.Count > 1) main += Spacing * (Children.Count - 1);
        return Vertical ? new Size(cross, main) : new Size(main, cross);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double mainAvailable = Vertical ? finalSize.Height : finalSize.Width;
        double crossAvailable = Vertical ? finalSize.Width : finalSize.Height;

        double fixedSum = 0, growSum = 0;
        foreach (var child in Children)
        {
            fixedSum += MainOf(child.DesiredSize);
            growSum += GrowOf(child);
        }
        double spacing = Spacing * Math.Max(0, Children.Count - 1);
        double remaining = Math.Max(0, mainAvailable - fixedSum - spacing);

        double position = 0;
        foreach (var child in Children)
        {
            double grow = GrowOf(child);
            double main = MainOf(child.DesiredSize) + (grow > 0 ? remaining * grow / growSum : 0);

            var align = GetSelfAlign(child) ?? CrossAlign;
            bool fillCross = align == Primitives.Align.Stretch || (Vertical ? GetFillWidth(child) : GetFillHeight(child));
            double natural = Vertical ? child.DesiredSize.Width : child.DesiredSize.Height;
            double cross = fillCross ? crossAvailable : Math.Min(natural, crossAvailable);
            double offset = align switch
            {
                Primitives.Align.Center => (crossAvailable - cross) / 2,
                Primitives.Align.End => crossAvailable - cross,
                _ => 0,
            };

            child.Arrange(Vertical
                ? new Rect(offset, position, cross, main)
                : new Rect(position, offset, main, cross));
            position += main + Spacing;
        }
        return finalSize;
    }

    double MainOf(Size size) => Vertical ? size.Height : size.Width;

    /// <summary>Un enfant qui remplit l'axe de la pile sans part déclarée en reçoit une.</summary>
    double GrowOf(Control child) =>
        GetGrow(child) is > 0 and var grow ? grow : (Vertical ? GetFillHeight(child) : GetFillWidth(child)) ? 1 : 0;
}
