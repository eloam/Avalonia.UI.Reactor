using System.Collections;

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Liste observée : un <c>Body</c> qui la parcourt est recalculé quand on ajoute, retire ou remplace un élément.
/// S'initialise comme une liste : <c>readonly StateList&lt;Todo&gt; todos = [a, b];</c>
/// </summary>
public sealed class StateList<T> : IList<T>, IReadOnlyList<T>
{
    readonly List<T> _items = [];
    readonly Signal _signal = new();

    public StateList() { }
    public StateList(IEnumerable<T> items) => _items.AddRange(items);

    public int Count { get { _signal.Read(); return _items.Count; } }

    public T this[int index]
    {
        get { _signal.Read(); return _items[index]; }
        set { _items[index] = value; _signal.Notify(); }
    }

    public void Add(T item) { _items.Add(item); _signal.Notify(); }
    public void AddRange(IEnumerable<T> items) { _items.AddRange(items); _signal.Notify(); }
    public void Insert(int index, T item) { _items.Insert(index, item); _signal.Notify(); }
    public void RemoveAt(int index) { _items.RemoveAt(index); _signal.Notify(); }
    public void Clear() { _items.Clear(); _signal.Notify(); }
    public void Move(int from, int to)
    {
        var item = _items[from];
        _items.RemoveAt(from);
        _items.Insert(to, item);
        _signal.Notify();
    }

    public bool Remove(T item)
    {
        if (!_items.Remove(item)) return false;
        _signal.Notify();
        return true;
    }

    public int RemoveAll(Predicate<T> match)
    {
        int n = _items.RemoveAll(match);
        if (n > 0) _signal.Notify();
        return n;
    }

    public int IndexOf(T item) { _signal.Read(); return _items.IndexOf(item); }
    public bool Contains(T item) { _signal.Read(); return _items.Contains(item); }
    public void CopyTo(T[] array, int arrayIndex) { _signal.Read(); _items.CopyTo(array, arrayIndex); }

    public List<T>.Enumerator GetEnumerator() { _signal.Read(); return _items.GetEnumerator(); }
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    bool ICollection<T>.IsReadOnly => false;
}
