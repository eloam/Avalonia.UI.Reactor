using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

public sealed record StackView(Axis Axis, IReadOnlyList<View> Children) : View
{
    /// <summary>Espace entre les enfants ; null = espacement du thème (<c>Theme.Spacing</c>).</summary>
    public float? Spacing { get; init; }
    public Align CrossAlign { get; init; }
}
