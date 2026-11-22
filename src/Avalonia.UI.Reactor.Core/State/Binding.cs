// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Represents a two-way reference to a value: a control reads the value and writes it back itself.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <remarks>
/// <para>
/// A binding is generated for every settable <see cref="StateAttribute">[State]</see> property, under <c>Bind</c>:
/// </para>
/// <code>
/// TextField(Bind.Draft, "What needs to be done?")
/// Checkbox(todo.Bind.Done)
/// </code>
/// <para>
/// A binding can also refer to any other value, for example a computed one:
/// <c>new Binding&lt;int&gt;(() =&gt; Page + 1, v =&gt; Page = v - 1)</c>.
/// </para>
/// </remarks>
/// <param name="get">The function that reads the value.</param>
/// <param name="set">The function that writes the value.</param>
public readonly struct Binding<T>(Func<T> get, Action<T> set) : IBinding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Binding{T}"/> struct that reads and writes a property of an object.
    /// </summary>
    /// <remarks>
    /// The generated bindings use this constructor: a control bound to the property is identified by it among its
    /// siblings, so that it keeps its control, focus and caret when a view appears or disappears before it.
    /// </remarks>
    /// <param name="get">The function that reads the value.</param>
    /// <param name="set">The function that writes the value.</param>
    /// <param name="owner">The object that has the property.</param>
    /// <param name="property">The name of the property.</param>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> or <paramref name="property"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="property"/> is empty.</exception>
    public Binding(Func<T> get, Action<T> set, object owner, string property)
        : this(get, set)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrEmpty(property);
        Target = new BindingTarget(owner, property);
    }

    /// <summary>
    /// Gets the current value.
    /// </summary>
    public T Value => get();

    /// <summary>
    /// Gets the property that the binding reads and writes, or <see langword="null"/> for a computed binding.
    /// </summary>
    internal BindingTarget? Target { get; }

    /// <inheritdoc/>
    BindingTarget? IBinding.Target => Target;

    /// <summary>
    /// Writes a new value.
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <remarks>
    /// This method can be passed where an <see cref="Action{T}"/> is expected: <c>TextField(text.Value, text.Set)</c>.
    /// </remarks>
    public void Set(T value) => set(value);
}
