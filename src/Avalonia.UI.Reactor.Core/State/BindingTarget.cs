// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Represents the property that a binding reads and writes: its owner and its name.
/// </summary>
/// <remarks>
/// A control bound to a property is identified by it among its siblings, so that it keeps its control, focus and caret
/// when a view appears or disappears before it.
/// </remarks>
/// <param name="Owner">The object that has the property.</param>
/// <param name="Property">The name of the property.</param>
internal readonly record struct BindingTarget(object Owner, string Property);
