using Mvu.Primitives;

namespace Mvu.Views;

/// <summary>Enfants superposés, le dernier au-dessus (badges, voiles, fenêtres modales).</summary>
public sealed record ZStackView(IReadOnlyList<View> Children) : View
{
    public Alignment Alignment { get; init; } = Alignment.Center;
}
