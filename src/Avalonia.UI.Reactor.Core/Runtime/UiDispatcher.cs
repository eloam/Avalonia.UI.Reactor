using System.Collections.Concurrent;

namespace Avalonia.UI.Reactor.Runtime;

/// <summary>
/// Contexte de synchronisation du thread d'interface : les suites d'<c>await</c> lancées
/// depuis une action reviennent ici, puis l'interface est mise à jour.
/// Une exception levée par ce travail (y compris dans un <c>async void</c>) est signalée, pas fatale.
/// </summary>
public sealed class UiDispatcher(Action wake, Action afterWork, Action<Exception> onError) : SynchronizationContext
{
    readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();

    /// <summary>Signale une erreur survenue sur le thread d'interface.</summary>
    public void Report(Exception e) => onError(e);

    public override void Post(SendOrPostCallback d, object? state)
    {
        _queue.Enqueue((d, state));
        wake();
    }

    public override void Send(SendOrPostCallback d, object? state) =>
        throw new NotSupportedException("Synchronous dispatch to the UI thread is not supported. Use 'Post' or 'await' from other threads.");

    /// <summary>Exécute le travail en attente sur le thread d'interface (appelé par la boucle de l'hôte).</summary>
    public void Drain()
    {
        bool any = false;
        while (_queue.TryDequeue(out var work))
        {
            try { work.Callback(work.State); }
            catch (Exception e) { onError(e); }
            any = true;
        }
        if (any) afterWork();
    }
}
