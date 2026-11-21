// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Primitives;

/// <summary>
/// Represents a resolved font.
/// </summary>
/// <param name="Family">The font family name.</param>
/// <param name="Size">The font size, in device-independent units.</param>
/// <param name="Weight">The weight on the OpenType scale, from 100 (thin) to 900 (black); 400 is regular.</param>
public readonly record struct Font(string Family, float Size, int Weight);
