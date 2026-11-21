// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Primitives;

/// <summary>
/// Represents a size, in device-independent units.
/// </summary>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct Size(float Width, float Height);
