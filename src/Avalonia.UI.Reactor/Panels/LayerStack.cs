// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Panels;

/// <summary>
/// Represents the panel of <c>ZStack</c> and <c>Overlay</c>: the children are layered, the last one on top.
/// </summary>
/// <remarks>
/// Each child takes its natural size and is placed according to the alignment of the stack, or stretched if it fills.
/// The natural size of the stack is that of its largest child.
/// </remarks>
internal sealed class LayerStack : Panel
{
    /// <summary>
    /// Identifies the <see cref="Alignment"/> property.
    /// </summary>
    public static readonly StyledProperty<Primitives.Alignment> AlignmentProperty =
        AvaloniaProperty.Register<LayerStack, Primitives.Alignment>(nameof(Alignment), Primitives.Alignment.Center);

    static LayerStack()
    {
        AffectsArrange<LayerStack>(AlignmentProperty);
        AffectsParentMeasure<LayerStack>(Flex.ChildProperties);
    }

    /// <summary>
    /// Gets or sets the position of the children that are smaller than the stack.
    /// </summary>
    public Primitives.Alignment Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0;
        double height = 0;
        foreach (Control child in Children)
        {
            child.Measure(availableSize);
            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        Primitives.Alignment alignment = Alignment;
        foreach (Control child in Children)
        {
            (double x, double width) = Flex.Place(
                finalSize.Width,
                child.DesiredSize.Width,
                Flex.GetFillWidth(child),
                Flex.GetSelfAlign(child) ?? alignment.Horizontal);
            (double y, double height) = Flex.Place(
                finalSize.Height,
                child.DesiredSize.Height,
                Flex.GetFillHeight(child),
                alignment.Vertical);
            child.Arrange(new Rect(x, y, width, height));
        }

        return finalSize;
    }
}
