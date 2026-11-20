namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// <c>ZStack</c>, <c>Overlay</c> et l'enveloppe des modificateurs d'un composant : une <see cref="LayerStack"/>.
/// Elle est aussi souple que son contenu : un enfant qui remplit ou qui a une part la lui donne.
/// </summary>
internal sealed class ZStackAdapter : ViewAdapter<ZStackView, LayerStack>
{
    public static readonly ZStackAdapter Instance = new();

    protected override void Update(Reconciler reconciler, Node node, LayerStack layers, ZStackView view, ViewEnvironment env)
    {
        layers.Alignment = view.Alignment;
        var children = env with { Font = env.FontFor(view), Foreground = env.ForegroundFor(view) };
        Reconciler.Arrange(layers.Children, reconciler.ReconcileChildren(node, view.Children, children));
    }

    protected override (bool Width, bool Height) FlexOf(Node node, ZStackView view) =>
        (node.Children.Any(c => c.FillWidth), node.Children.Any(c => c.FillHeight));

    public override float GrowOf(Node node) => node.Children.Select(c => c.Grow).DefaultIfEmpty(0).Max();
}
