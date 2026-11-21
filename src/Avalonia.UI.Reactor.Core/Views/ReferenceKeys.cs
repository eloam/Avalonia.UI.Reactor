// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Provides a stable key for an object: the same key as long as the object is alive, without keeping it alive.
/// </summary>
/// <remarks>
/// It identifies a list item without an identifier, a component or the owner of a binding among the children of a
/// container.
/// </remarks>
internal static class ReferenceKeys
{
    private static readonly ConditionalWeakTable<object, string> s_keys = [];
    private static int s_next;

    /// <summary>
    /// Returns the key of an object.
    /// </summary>
    /// <param name="item">The object.</param>
    /// <returns>The key of the object, the same on every call.</returns>
    public static string Of(object item) => s_keys.GetValue(item, _ => $"#{Interlocked.Increment(ref s_next)}");
}
