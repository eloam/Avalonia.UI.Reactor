namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary><c>Box()</c> : une surface vide, faite de ses seuls modificateurs (taille, fond, bordure, angles, clic).</summary>
internal sealed class BoxAdapter : ViewAdapter<BoxView, Surface>
{
    public static readonly BoxAdapter Instance = new();

    public override bool DrawsDecoration => true;

    protected override void Attach(Reconciler reconciler, Node node, Surface box) =>
        box.TapAction = () => reconciler.Run(node.View.TapAction);

    protected override void Update(Reconciler reconciler, Node node, Surface box, BoxView view, ViewEnvironment env) =>
        Reconciler.ApplyDecoration(box, view, env.Theme);
}
