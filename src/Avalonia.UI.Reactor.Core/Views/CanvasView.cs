using Avalonia.UI.Reactor.Rendering;

namespace Avalonia.UI.Reactor.Views;

/// <summary>Dessin libre : graphiques, jauges, icônes… Taille fixée par <c>.Size()</c> ou <c>.FillWidth()</c>.</summary>
public sealed record CanvasView(Action<DrawScope> Draw) : View;
