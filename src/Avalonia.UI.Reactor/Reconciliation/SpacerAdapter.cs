using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary><c>Spacer()</c> : un contrôle vide ; sa part de place (<c>Grow = 1</c>) vient de la vue, comme pour toute vue.</summary>
internal sealed class SpacerAdapter : ViewAdapter<SpacerView, Control>
{
    public static readonly SpacerAdapter Instance = new();

    protected override void Update(Reconciler reconciler, Node node, Control spacer, SpacerView view, ViewEnvironment env) { }
}
