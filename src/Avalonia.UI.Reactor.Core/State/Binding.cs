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
public readonly struct Binding<T>(Func<T> get, Action<T> set)
{
    /// <summary>
    /// Gets the current value.
    /// </summary>
    public T Value => get();

    /// <summary>
    /// Writes a new value.
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <remarks>
    /// This method can be passed where an <see cref="Action{T}"/> is expected: <c>TextField(text.Value, text.Set)</c>.
    /// </remarks>
    public void Set(T value) => set(value);
}
