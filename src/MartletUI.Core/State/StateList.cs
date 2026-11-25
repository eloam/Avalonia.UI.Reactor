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
/// A state list is initialized like a list: <c>StateList&lt;Todo&gt; Todos { get; } = [a, b];</c>. Changing an item
/// itself (a property of a <c>Todo</c>) is observed through the <c>[State]</c> properties of the item.
/// </remarks>
public sealed class StateList<T> : IList<T>, IReadOnlyList<T>
{
    private readonly List<T> _items = [];
    private readonly Signal _signal = new($"StateList<{typeof(T).Name}>");

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
            return _items.Count;
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
    /// A binding whose item has been removed reads the item now at its index, and writing through it does nothing.
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
            int count = Count;
            var bindings = new Binding<T>[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                bindings[i] = new Binding<T>(
                    () => this[index],
                    value => SetIfPresent(index, value),
                    this,
                    string.Create(CultureInfo.InvariantCulture, $"[{index}]"));
            }

            return bindings;
        }
    }

    /// <inheritdoc/>
    bool ICollection<T>.IsReadOnly => false;

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
            return _items[index];
        }

        set
        {
            _signal.EnsureWritable();
            _items[index] = value;
            _signal.Notify();
        }
    }

    /// <summary>
    /// Adds an item at the end of the list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        _signal.EnsureWritable();
        _items.Add(item);
        _signal.Notify();
    }

    /// <summary>
    /// Adds items at the end of the list, with a single notification.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<T> items)
    {
        _signal.EnsureWritable();
        _items.AddRange(items);
        _signal.Notify();
    }

    /// <summary>
    /// Inserts an item at an index.
    /// </summary>
    /// <param name="index">The zero-based index where the item is inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, T item)
    {
        _signal.EnsureWritable();
        _items.Insert(index, item);
        _signal.Notify();
    }

    /// <summary>
    /// Removes the item at an index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        _signal.EnsureWritable();
        _items.RemoveAt(index);
        _signal.Notify();
    }

    /// <summary>
    /// Removes all items.
    /// </summary>
    public void Clear()
    {
        _signal.EnsureWritable();
        _items.Clear();
        _signal.Notify();
    }

    /// <summary>
    /// Moves an item to another index, with a single notification.
    /// </summary>
    /// <param name="from">The current index of the item.</param>
    /// <param name="to">The new index of the item.</param>
    public void Move(int from, int to)
    {
        _signal.EnsureWritable();
        T item = _items[from];
        _items.RemoveAt(from);
        _items.Insert(to, item);
        _signal.Notify();
    }

    /// <summary>
    /// Removes the first occurrence of an item.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns><see langword="true"/> if the item was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(T item)
    {
        _signal.EnsureWritable();
        if (!_items.Remove(item))
        {
            return false;
        }

        _signal.Notify();
        return true;
    }

    /// <summary>
    /// Removes all the items that match a condition, with a single notification.
    /// </summary>
    /// <param name="match">The condition of the items to remove.</param>
    /// <returns>The number of items removed.</returns>
    public int RemoveAll(Predicate<T> match)
    {
        _signal.EnsureWritable();
        int removed = _items.RemoveAll(match);
        if (removed > 0)
        {
            _signal.Notify();
        }

        return removed;
    }

    /// <summary>
    /// Returns the index of the first occurrence of an item.
    /// </summary>
    /// <param name="item">The item to look for.</param>
    /// <returns>The zero-based index of the item, or -1 if the list does not contain it.</returns>
    public int IndexOf(T item)
    {
        _signal.Read();
        return _items.IndexOf(item);
    }

    /// <summary>
    /// Determines whether the list contains an item.
    /// </summary>
    /// <param name="item">The item to look for.</param>
    /// <returns><see langword="true"/> if the list contains the item; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T item)
    {
        _signal.Read();
        return _items.Contains(item);
    }

    /// <summary>
    /// Copies the items to an array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The index in <paramref name="array"/> where copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        _signal.Read();
        _items.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Returns an enumerator over the items.
    /// </summary>
    /// <returns>An enumerator over the items.</returns>
    public List<T>.Enumerator GetEnumerator()
    {
        _signal.Read();
        return _items.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Writes through a binding of Bind: an item removed meanwhile (the control kept its text) is left as it is.
    private void SetIfPresent(int index, T value)
    {
        if (index < _items.Count)
        {
            this[index] = value;
        }
    }
}
