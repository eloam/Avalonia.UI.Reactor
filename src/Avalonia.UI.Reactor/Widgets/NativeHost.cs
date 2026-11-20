using Avalonia.Controls;
using Avalonia.Threading;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.State;

/// <summary>
/// Porte le contrôle d'un <c>Native(…)</c>. Sa mise à jour a sa propre portée : un état lu par elle la fait refaire quand
/// il change, sans réévaluer la vue qui contient le contrôle (comme le dessin d'un <c>Canvas</c>).
/// </summary>
internal sealed class NativeHost : Decorator
{
    /// <summary>Nom de la portée de la mise à jour (elle n'a pas de place dans l'arbre des portées).</summary>
    const string UpdateScopeId = "native";

    readonly Scope _updates;
    Action? _update;
    int _rerunPending;

    public NativeHost() => _updates = new Scope(RequestRerun, UpdateScopeId, identity: null);

    /// <summary>Applique la mise à jour de la vue courante au contrôle, en suivant les états qu'elle lit.</summary>
    public void Apply(Action update)
    {
        _update = update;
        _updates.Evaluate(update);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _updates.Dispose();
    }

    /// <summary>Un état lu par la mise à jour a changé (n'importe quel thread) : la refaire une fois, sur le thread d'interface.</summary>
    void RequestRerun()
    {
        if (Interlocked.Exchange(ref _rerunPending, 1) == 1) return;
        Dispatcher.UIThread.Post(() =>
        {
            Interlocked.Exchange(ref _rerunPending, 0);
            if (_update is { } update) _updates.Evaluate(update);
        });
    }
}
