using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Panels;

/// <summary>
/// Couches de <c>ZStack</c> / <c>Overlay</c> : les enfants sont superposés (le dernier au-dessus), chacun à sa taille
/// naturelle et placé selon l'alignement de la pile, ou étiré s'il remplit. La taille naturelle est celle du plus grand.
/// </summary>
internal sealed class LayerStack : Panel
{
    public static readonly StyledProperty<Primitives.Alignment> AlignmentProperty =
        AvaloniaProperty.Register<LayerStack, Primitives.Alignment>(nameof(Alignment), Primitives.Alignment.Center);

    static LayerStack()
    {
        AffectsArrange<LayerStack>(AlignmentProperty);
        AffectsParentMeasure<LayerStack>(Flex.ChildProperties);
    }

    public Primitives.Alignment Alignment { get => GetValue(AlignmentProperty); set => SetValue(AlignmentProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var alignment = Alignment;
        foreach (var child in Children)
        {
            var (x, width) = Flex.Place(finalSize.Width, child.DesiredSize.Width, Flex.GetFillWidth(child),
                Flex.GetSelfAlign(child) ?? alignment.Horizontal);
            var (y, height) = Flex.Place(finalSize.Height, child.DesiredSize.Height, Flex.GetFillHeight(child),
                alignment.Vertical);
            child.Arrange(new Rect(x, y, width, height));
        }
        return finalSize;
    }
}
