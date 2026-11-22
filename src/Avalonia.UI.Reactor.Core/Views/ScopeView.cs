// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents a part of the view re-evaluated on its own: a component, a view method called in a block, a
/// <c>ForEach</c> item or an <c>Observe</c> block.
/// </summary>
/// <remarks>
/// Only the state read in <see cref="Build"/> makes it re-evaluate. Modifiers apply to its content.
/// </remarks>
/// <param name="Build">The function that builds the content of the scope.</param>
public sealed record ScopeView(Func<View> Build) : View
{
    private static readonly ConditionalWeakTable<ScopeView, ZStackView> s_wrappers = [];

    /// <summary>
    /// Gets what the scope displays: a component instance, whose state survives the re-evaluations of its parent, or
    /// the name of a view method.
    /// </summary>
    internal object? Identity { get; init; }

    /// <summary>
    /// Gets the values that the content depends on besides the state it reads: the arguments of a view method.
    /// </summary>
    /// <remarks>
    /// When its parent is re-evaluated, a scope with inputs is re-evaluated only if they changed (<see cref="object.Equals(object?, object?)"/>),
    /// as SwiftUI compares the inputs of a view. Without inputs, a scope that is not a component is re-evaluated with its
    /// parent.
    /// </remarks>
    internal object? Inputs { get; init; }

    /// <summary>
    /// Returns the wrapper that carries the modifiers set on the scope (<c>counter.Padding(8)</c>).
    /// </summary>
    /// <remarks>
    /// The modifiers apply to a wrapper around the content, aligned to the top left. The wrapper is cached so that the
    /// same scope always gives the same instance: the engine compares views by reference.
    /// </remarks>
    /// <returns>The wrapper, or <see langword="null"/> if the scope has no modifier.</returns>
    internal ZStackView? ModifierWrapper()
    {
        var plain = new ScopeView(Build) { Identity = Identity, Inputs = Inputs };
        if (this with { Key = null } == plain)
        {
            return null;
        }

        return s_wrappers.GetValue(this, scope =>
            new ZStackView([plain]) { Alignment = Alignment.TopLeft }.CopyFrom(scope) with { Key = null });
    }
}
