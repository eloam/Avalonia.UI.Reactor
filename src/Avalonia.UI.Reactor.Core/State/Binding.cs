namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Liaison à double sens vers un état : le contrôle lit la valeur et la modifie lui-même.
/// Générée pour chaque propriété <c>[State]</c> modifiable, sous <c>Bind</c> :
/// <code>
/// TextField(Bind.Draft, "Que faut-il faire ?")
/// Checkbox(todo.Bind.Done)
/// </code>
/// </summary>
public readonly struct Binding<T>
{
    readonly Func<T> _get;
    readonly Action<T> _set;

    public Binding(Func<T> get, Action<T> set)
    {
        _get = get;
        _set = set;
    }

    public T Value
    {
        get => _get();
        set => _set(value);
    }

    public void Set(T value) => _set(value);
}
