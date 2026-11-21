// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents the size and the weight of a text style.
/// </summary>
/// <param name="Size">The font size, in device-independent units.</param>
/// <param name="Weight">The weight on the OpenType scale, from 100 (thin) to 900 (black); 400 is regular.</param>
public sealed record TextStyleSpec(float Size, int Weight = FontWeight.Regular);
