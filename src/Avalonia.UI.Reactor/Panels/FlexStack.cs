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

    static FlexStack()
    {
        AffectsMeasure<FlexStack>(AxisProperty, SpacingProperty);
        AffectsArrange<FlexStack>(CrossAlignProperty);
        AffectsParentMeasure<FlexStack>(Flex.ChildProperties);
    }

    public Primitives.Axis Axis { get => GetValue(AxisProperty); set => SetValue(AxisProperty, value); }
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public Primitives.Align CrossAlign { get => GetValue(CrossAlignProperty); set => SetValue(CrossAlignProperty, value); }

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
            main += MainOf(child.DesiredSize);
            cross = Math.Max(cross, CrossOf(child.DesiredSize));
        }
        if (Children.Count > 1) main += Spacing * (Children.Count - 1);
        return Vertical ? new Size(cross, main) : new Size(main, cross);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double mainAvailable = MainOf(finalSize);
        double crossAvailable = CrossOf(finalSize);

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
            bool fillCross = Vertical ? Flex.GetFillWidth(child) : Flex.GetFillHeight(child);
            var (offset, cross) = Flex.Place(crossAvailable, CrossOf(child.DesiredSize), fillCross,
                Flex.GetSelfAlign(child) ?? CrossAlign);

            child.Arrange(Vertical
                ? new Rect(offset, position, cross, main)
                : new Rect(position, offset, main, cross));
            position += main + Spacing;
        }
        return finalSize;
    }

    double MainOf(Size size) => Vertical ? size.Height : size.Width;

    double CrossOf(Size size) => Vertical ? size.Width : size.Height;

    /// <summary>Un enfant qui remplit l'axe de la pile sans part déclarée en reçoit une.</summary>
    double GrowOf(Control child) =>
        Flex.GetGrow(child) is > 0 and var grow ? grow : (Vertical ? Flex.GetFillHeight(child) : Flex.GetFillWidth(child)) ? 1 : 0;
}
