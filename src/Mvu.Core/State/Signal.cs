namespace Mvu.State;

/// <summary>
/// Source de changement observable : brique de base de <c>[State]</c> et de <see cref="StateList{T}"/>.
/// <see cref="Read"/> abonne la portée en cours de calcul, <see cref="Notify"/> invalide les abonnés.
/// </summary>
public sealed class Signal
{
    readonly HashSet<Scope> _subscribers = [];

    public void Read()
    {
        if (Scope.Current is not { } scope) return;
        lock (_subscribers) _subscribers.Add(scope);
        scope.Track(this);
    }

    public void Notify()
    {
        Scope[] subscribers;
        lock (_subscribers)
        {
            if (_subscribers.Count == 0) return;
            subscribers = [.. _subscribers];
        }
        foreach (var s in subscribers) s.MarkDirty();
    }

    internal void Unsubscribe(Scope scope)
    {
        lock (_subscribers) _subscribers.Remove(scope);
    }
}
