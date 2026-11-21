// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Rendering;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents free drawing: charts, gauges, icons. Its size is set with <c>.Size()</c> or <c>.FillWidth()</c>.
/// </summary>
/// <param name="Draw">The function that draws the canvas; it is called again when the state it reads changes.</param>
public sealed record CanvasView(Action<DrawScope> Draw) : View;
