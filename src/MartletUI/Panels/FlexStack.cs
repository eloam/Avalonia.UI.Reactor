// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;

namespace MartletUI.Panels;

/// <summary>
/// Represents the panel of <c>VStack</c> and <c>HStack</c>, with the rules of SwiftUI.
/// </summary>
/// <remarks>
/// Each child takes its natural size on the axis of the stack; the remaining space goes to the children that stretch
/// (<c>Spacer()</c>, <c>.Grow()</c>, <c>.FillWidth()</c>) in proportion to their share. On the other axis, a child is
/// aligned (start, center, end) or stretched. The wrapper that the reconciler places around the panel draws its
/// background, border, corners and padding.
/// </remarks>
internal sealed class FlexStack : Panel
{
    /// <summary>
    /// Identifies the <see cref="Axis"/> property.
    /// </summary>
    public static readonly StyledProperty<Primitives.Axis> AxisProperty =
        AvaloniaProperty.Register<FlexStack, Primitives.Axis>(nameof(Axis), Primitives.Axis.Vertical);

    /// <summary>
    /// Identifies the <see cref="Spacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<FlexStack, double>(nameof(Spacing));

    /// <summary>
    /// Identifies the <see cref="CrossAlign"/> property.
    /// </summary>
    public static readonly StyledProperty<Primitives.Align> CrossAlignProperty =
        AvaloniaProperty.Register<FlexStack, Primitives.Align>(nameof(CrossAlign));

    static FlexStack()
    {
        AffectsMeasure<FlexStack>(AxisProperty, SpacingProperty);
        AffectsArrange<FlexStack>(CrossAlignProperty);
        AffectsParentMeasure<FlexStack>(Flex.ChildProperties);
    }

    /// <summary>
    /// Gets or sets the direction of the stack.
    /// </summary>
    public Primitives.Axis Axis
    {
        get => GetValue(AxisProperty);
        set => SetValue(AxisProperty, value);
    }

    /// <summary>
    /// Gets or sets the space between the children.
    /// </summary>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the alignment of the children on the cross axis.
    /// </summary>
    public Primitives.Align CrossAlign
    {
        get => GetValue(CrossAlignProperty);
        set => SetValue(CrossAlignProperty, value);
    }

    private bool Vertical => Axis == Primitives.Axis.Vertical;

    // The displayed children: a hidden child (a menu bar handed to the system) takes neither space nor spacing.
    private IEnumerable<Control> Shown => Children.Where(child => child.IsVisible);

    /// <summary>
    /// Measures the stack: the sum of the children on the axis of the stack, plus the spacing, and the largest child on
    /// the other axis.
    /// </summary>
    /// <param name="availableSize">The space available to the stack.</param>
    /// <returns>The natural size of the stack.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        // Each child gives its natural size on the axis of the stack; on the other axis, it knows the space offered.
        Size offered = Vertical
            ? new Size(availableSize.Width, double.PositiveInfinity)
            : new Size(double.PositiveInfinity, availableSize.Height);

        double main = 0;
        double cross = 0;
        foreach (Control child in Shown)
        {
            child.Measure(offered);
            main += MainOf(child.DesiredSize);
            cross = Math.Max(cross, CrossOf(child.DesiredSize));
        }

        int shown = Shown.Count();
        if (shown > 1)
        {
            main += Spacing * (shown - 1);
        }

        return Vertical ? new Size(cross, main) : new Size(main, cross);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        double mainAvailable = MainOf(finalSize);
        double crossAvailable = CrossOf(finalSize);

        double fixedSum = 0;
        double growSum = 0;
        foreach (Control child in Shown)
        {
            fixedSum += MainOf(child.DesiredSize);
            growSum += GrowOf(child);
        }

        double spacing = Spacing * Math.Max(0, Shown.Count() - 1);
        double remaining = Math.Max(0, mainAvailable - fixedSum - spacing);

        double position = 0;
        foreach (Control child in Shown)
        {
            double grow = GrowOf(child);
            double main = MainOf(child.DesiredSize) + (grow > 0 ? remaining * grow / growSum : 0);
            bool fillCross = Vertical ? Flex.GetFillWidth(child) : Flex.GetFillHeight(child);
            (double offset, double cross) = Flex.Place(
                crossAvailable,
                CrossOf(child.DesiredSize),
                fillCross,
                Flex.GetSelfAlign(child) ?? CrossAlign);

            child.Arrange(Vertical
                ? new Rect(offset, position, cross, main)
                : new Rect(position, offset, main, cross));
            position += main + Spacing;
        }

        return finalSize;
    }

    private double MainOf(Size size) => Vertical ? size.Height : size.Width;

    private double CrossOf(Size size) => Vertical ? size.Width : size.Height;

    // A child that fills the axis of the stack without a declared share receives one.
    private double GrowOf(Control child)
    {
        if (Flex.GetGrow(child) is > 0 and var grow)
        {
            return grow;
        }

        return (Vertical ? Flex.GetFillHeight(child) : Flex.GetFillWidth(child)) ? 1 : 0;
    }
}
