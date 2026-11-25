// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections;
using System.Globalization;

namespace MartletUI.State;

/// <summary>
/// Represents an observed list: a <c>Body</c> that reads it is re-evaluated when an item is added, removed or replaced.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
/// <remarks>
/// <para>
/// A state list is initialized like a list: <c>StateList&lt;Todo&gt; Todos { get; } = [a, b];</c>. Changing an item
/// itself (a property of a <c>Todo</c>) is observed through the <c>[State]</c> properties of the item.
/// </para>
/// <para>
/// A state list can be read and changed from any thread. An enumeration, with <c>foreach</c>, LINQ or <c>ForEach</c>, goes
/// through the items as they were when it started. It does not implement <see cref="IList{T}"/>, whose users read the
/// count then the items, which another thread can change in between. For the same reason, copy a list that another thread
/// changes before indexing it up to <see cref="Count"/>, or spreading it among other elements:
/// <c>Todo[] copy = [.. Todos];</c>.
/// </para>
/// </remarks>
public sealed class StateList<T> : IReadOnlyList<T>
{
    private readonly Lock _lock = new();
    private readonly List<T> _items = [];
    private readonly Signal _signal = new($"StateList<{typeof(T).Name}>");

    // The items as the readers see them, never modified: copied by the first read that follows a change.
    private T[]? _snapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateList{T}"/> class that is empty.
    /// </summary>
    public StateList()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateList{T}"/> class that contains the given items.
    /// </summary>
    /// <param name="items">The initial items.</param>
    public StateList(IEnumerable<T> items)
    {
        _items.AddRange(items);
    }

    /// <summary>
    /// Gets the number of items.
    /// </summary>
    public int Count
    {
        get
        {
            _signal.Read();
            lock (_lock)
            {
                return _items.Count;
            }
        }
    }

    /// <summary>
    /// Gets a binding to each item, in order: a row of a list edits its item through it, as with <c>ForEach($tags)</c>
    /// in SwiftUI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reading the bindings subscribes the view to the list. In <c>ForEach</c>, a row is keyed by the index of its item,
    /// like the controls bound to it: when an item is removed, the rows that follow show the items that moved up. A key
    /// function keeps a row with its item instead: <c>ForEach(Tags.Bind, tag =&gt; TextField(tag), tag =&gt; tag.Value)</c>.
    /// </para>
    /// <para>
    /// A binding whose item has been removed reads the item now at its index, or its last value if there is none, and
    /// writing through it then does nothing.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// ForEach(Tags.Bind, tag =&gt; TextField(tag))
    /// </code>
    /// </example>
    public IReadOnlyList<Binding<T>> Bind
    {
        get
        {
            T[] items = Read();
            var bindings = new Binding<T>[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                bindings[i] = new Binding<T>(
                    () => ItemOrLast(index, items[index]),
                    value => SetIfPresent(index, value),
                    this,
                    string.Create(CultureInfo.InvariantCulture, $"[{index}]"));
            }

            return bindings;
        }
    }

    /// <summary>
    /// Gets or sets the item at an index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at <paramref name="index"/>.</returns>
    public T this[int index]
    {
        get
        {
            _signal.Read();
            lock (_lock)
            {
                return _items[index];
            }
        }

        set => Change(items => items[index] = value);
    }

    /// <summary>
    /// Adds an item at the end of the list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item) => Change(items => items.Add(item));

    /// <summary>
    /// Adds items at the end of the list, with a single notification.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<T> items)
    {
        // Enumerated before the change: it may run the code of the application, or be this list.
        T[] added = [.. items];
        Change(list => list.AddRange(added));
    }

    /// <summary>
    /// Inserts an item at an index.
    /// </summary>
    /// <param name="index">The zero-based index where the item is inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, T item) => Change(items => items.Insert(index, item));

    /// <summary>
    /// Removes the item at an index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index) => Change(items => items.RemoveAt(index));

    /// <summary>
    /// Removes all items.
    /// </summary>
    public void Clear() => Change(items => items.Clear());

    /// <summary>
    /// Moves an item to another index, with a single notification.
    /// </summary>
    /// <param name="from">The current index of the item.</param>
    /// <param name="to">The new index of the item.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="from"/> or <paramref name="to"/> is not the index of
    /// an item.</exception>
    public void Move(int from, int to) => Change(items =>
    {
        T item = items[from];
        ArgumentOutOfRangeException.ThrowIfNegative(to);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(to, items.Count);
        items.RemoveAt(from);
        items.Insert(to, item);
    });

    /// <summary>
    /// Removes the first occurrence of an item.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns><see langword="true"/> if the item was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(T item) => TryChange(items => items.Remove(item));

    /// <summary>
    /// Removes all the items that match a condition, with a single notification.
    /// </summary>
    /// <remarks>
    /// The other threads that use the list wait while the condition is evaluated.
    /// </remarks>
    /// <param name="match">The condition of the items to remove.</param>
    /// <returns>The number of items removed.</returns>
    public int RemoveAll(Predicate<T> match)
    {
        int removed = 0;
        TryChange(items => (removed = items.RemoveAll(match)) > 0);
        return removed;
    }

    /// <summary>
    /// Returns the index of the first occurrence of an item.
    /// </summary>
    /// <param name="item">The item to look for.</param>
    /// <returns>The zero-based index of the item, or -1 if the list does not contain it.</returns>
    public int IndexOf(T item) => Array.IndexOf(Read(), item);

    /// <summary>
    /// Determines whether the list contains an item.
    /// </summary>
    /// <param name="item">The item to look for.</param>
    /// <returns><see langword="true"/> if the list contains the item; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T item) => IndexOf(item) >= 0;

    /// <summary>
    /// Returns an enumerator over the items, as they are now.
    /// </summary>
    /// <returns>An enumerator over the items.</returns>
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Read()).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // The items, for a reader that depends on the list: the array must not be modified.
    private T[] Read()
    {
        _signal.Read();
        lock (_lock)
        {
            return _snapshot ??= [.. _items];
        }
    }

    private void Change(Action<List<T>> change) => TryChange(items =>
    {
        change(items);
        return true;
    });

    // Makes a change, then notifies the views that read the list, outside the lock, if the change returns true.
    private bool TryChange(Func<List<T>, bool> change)
    {
        _signal.EnsureWritable();
        lock (_lock)
        {
            _snapshot = null;
            if (!change(_items))
            {
                return false;
            }
        }

        _signal.Notify();
        return true;
    }

    // Reads through a binding of Bind: an item removed meanwhile has its last value.
    private T ItemOrLast(int index, T last)
    {
        T[] items = Read();
        return index < items.Length ? items[index] : last;
    }

    // Writes through a binding of Bind: an item removed meanwhile (the control kept its text) is left as it is.
    private void SetIfPresent(int index, T value) => TryChange(items =>
    {
        if (index >= items.Count)
        {
            return false;
        }

        items[index] = value;
        return true;
    });
}
