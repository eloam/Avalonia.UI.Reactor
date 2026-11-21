// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Provides the key that identifies an item among the rows of a <c>ForEach</c>.
/// </summary>
/// <remarks>
/// Implement <see cref="IIdentifiable{TId}"/>, which provides this key from its <see cref="IIdentifiable{TId}.Id"/>.
/// </remarks>
public interface IIdentifiable
{
    /// <summary>
    /// Gets the key that identifies the item, which stays the same when the item changes.
    /// </summary>
    public object Key { get; }
}
