// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents layered views, the last one on top: badges, overlays, modal panels.
/// </summary>
/// <param name="Children">The layers, from bottom to top.</param>
public sealed record ZStackView(IReadOnlyList<View> Children) : View
{
    /// <summary>
    /// Gets the position of the layers that are smaller than the stack.
    /// </summary>
    public Alignment Alignment { get; init; } = Alignment.Center;
}
