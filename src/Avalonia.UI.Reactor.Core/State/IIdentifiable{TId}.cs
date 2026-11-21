// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Provides the identifier of an item, which <c>ForEach</c> uses as the key of its row, like <c>Identifiable</c> in
/// SwiftUI.
/// </summary>
/// <remarks>
/// A row keeps its controls, focus and hover when the list changes, even when the item is a record rebuilt on every
/// update. A key given to <c>ForEach</c> takes precedence over the identifier.
/// </remarks>
/// <typeparam name="TId">The type of the identifier.</typeparam>
/// <example>
/// <code>
/// public sealed partial class Todo : IIdentifiable&lt;Guid&gt;
/// {
///     public Guid Id { get; } = Guid.NewGuid();
/// }
///
/// ForEach(Todos, Row)   // each row is keyed by todo.Id
/// </code>
/// </example>
public interface IIdentifiable<out TId> : IIdentifiable
    where TId : notnull
{
    /// <summary>
    /// Gets the identifier of the item, which stays the same when the item changes.
    /// </summary>
    public TId Id { get; }

    /// <inheritdoc/>
    object IIdentifiable.Key => Id;
}
