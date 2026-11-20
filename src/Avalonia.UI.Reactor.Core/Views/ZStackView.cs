using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

/// <summary>Enfants superposés, le dernier au-dessus (badges, voiles, fenêtres modales).</summary>
public sealed record ZStackView(IReadOnlyList<View> Children) : View
{
    public Alignment Alignment { get; init; } = Alignment.Center;
}
