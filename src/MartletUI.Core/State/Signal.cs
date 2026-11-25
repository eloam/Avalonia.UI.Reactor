// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.State;

/// <summary>
/// Represents an observable source of change: the building block of <see cref="StateAttribute">[State]</see>
/// properties and of <see cref="StateList{T}"/>.
/// </summary>
/// <remarks>
/// <see cref="Read"/> subscribes the scope being evaluated; <see cref="Notify"/> marks the subscribed scopes for
/// re-evaluation. Both methods can be called from any thread. The code generated for <c>[State]</c> properties uses this
/// type, which is why it is public.
/// </remarks>
public sealed class Signal
{
    private readonly HashSet<Scope> _subscribers = [];

    /// <summary>
    /// Records that the scope being evaluated, if any, depends on this signal.
    /// </summary>
    public void Read()
    {
        if (Scope.Current is not { } scope)
        {
            return;
        }

        lock (_subscribers)
        {
            _subscribers.Add(scope);
        }

        scope.Track(this);
    }

    /// <summary>
    /// Marks every scope that depends on this signal for re-evaluation.
    /// </summary>
    public void Notify()
    {
        Scope[] subscribers;
        lock (_subscribers)
        {
            if (_subscribers.Count == 0)
            {
                return;
            }

            subscribers = [.. _subscribers];
        }

        foreach (Scope subscriber in subscribers)
        {
            subscriber.MarkDirty();
        }
    }

    /// <summary>
    /// Removes a scope from the subscribers of this signal.
    /// </summary>
    /// <param name="scope">The scope that no longer depends on this signal.</param>
    internal void Unsubscribe(Scope scope)
    {
        lock (_subscribers)
        {
            _subscribers.Remove(scope);
        }
    }
}
