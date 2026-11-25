// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.State;

/// <summary>
/// Gives access to the target of a <see cref="Binding{T}"/> without knowing its type of value.
/// </summary>
/// <remarks>
/// <c>ForEach</c> keys a row by the target of its binding (<c>ForEach(Tags.Bind, …)</c>): a binding is a new value
/// on every update, so it cannot be its own key.
/// </remarks>
internal interface IBinding
{
    /// <summary>
    /// Gets the property that the binding reads and writes, or <see langword="null"/> for a computed binding.
    /// </summary>
    public BindingTarget? Target { get; }
}
