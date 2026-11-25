// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.State;

/// <summary>
/// Represents an observable source of change: the building block of <see cref="StateAttribute">[State]</see>
/// properties and of <see cref="StateList{T}"/>.
/// </summary>
/// <remarks>
/// <see cref="Read"/> subscribes the scope being evaluated; <see cref="Notify"/> marks the subscribed scopes for
/// re-evaluation; <see cref="EnsureWritable"/>, called before a change, rejects it while a view is computed. These methods
/// can be called from any thread. The code generated for <c>[State]</c> properties uses this type, which is why it is
/// public.
/// </remarks>
public sealed class Signal
{
    private readonly HashSet<Scope> _subscribers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Signal"/> class.
    /// </summary>
    /// <param name="name">The name of the state, for the messages of exceptions.</param>
    public Signal(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Change = $"The state '{name}' was changed";
    }

    /// <summary>
    /// Gets the description of a change, the start of the message of an exception.
    /// </summary>
    internal string Change { get; init; }

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
    /// Verifies that the state can change, before it does: a view that is being computed only reads state.
    /// </summary>
    /// <remarks>
    /// A change that no scope depends on is accepted: a view can fill the objects that it creates. A change made from
    /// another thread is accepted too, because it is not made by the view.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The view that is being computed on this thread changes a state that
    /// a view depends on.</exception>
    public void EnsureWritable()
    {
        if (Scope.Current is not { IsReadOnly: true } scope)
        {
            return;
        }

        lock (_subscribers)
        {
            if (_subscribers.Count == 0)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"{Change} while {scope.Description} was being computed, which would compute it again without end. " +
            "Change it in an action, in '.OnAppear(…)' or in '.Task(…)'.");
    }

    /// <summary>
    /// Marks every scope that depends on this signal for re-evaluation.
    /// </summary>
    /// <exception cref="InvalidOperationException">The view that is being computed on this thread changes a state that
    /// a view depends on.</exception>
    public void Notify()
    {
        EnsureWritable();

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
