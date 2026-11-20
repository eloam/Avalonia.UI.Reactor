namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary><c>VStack</c> / <c>HStack</c> : une <see cref="FlexStack"/>, espacement du thème sauf <c>.Spacing(…)</c> ; fond et bordure par l'enveloppe commune.</summary>
internal sealed class StackAdapter : ViewAdapter<StackView, FlexStack>
{
    public static readonly StackAdapter Instance = new();

    protected override void Update(Reconciler reconciler, Node node, FlexStack stack, StackView view, ViewEnvironment env)
    {
        var theme = env.Theme;
        stack.Axis = view.Axis;
        stack.Spacing = view.Spacing ?? theme.Spacing;
        stack.CrossAlign = view.CrossAlign;

        // Les enfants héritent du style de texte de la pile (.Font, .Foreground posés sur elle).
        var children = env with { Font = env.FontFor(view), Foreground = env.ForegroundFor(view) };
        Reconciler.Arrange(stack.Children, reconciler.ReconcileChildren(node, view.Children, children));
    }

    /// <summary>Une pile s'étire sur son axe si un enfant s'étire ou a une part ; sur l'autre, si un enfant le remplit.</summary>
    protected override (bool Width, bool Height) FlexOf(Node node, StackView view) => view.Axis == Primitives.Axis.Horizontal
        ? (node.Children.Any(c => c.Grow > 0 || c.FillWidth), node.Children.Any(c => c.FillHeight))
        : (node.Children.Any(c => c.FillWidth), node.Children.Any(c => c.Grow > 0 || c.FillHeight));
}
