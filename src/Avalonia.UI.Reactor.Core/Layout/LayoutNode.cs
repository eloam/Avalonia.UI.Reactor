using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Layout;

internal sealed class LayoutNode
{
    public required View View { get; init; }
    public required string Id { get; init; }
    // Valeurs résolues (vue ⊕ thème ⊕ héritage) — le rendu n'a plus à les recalculer.
    public required Theme Theme { get; init; }
    public Font Font { get; init; }
    public Color Foreground { get; init; }
    public Thickness Padding { get; init; }
    public float Radius { get; init; }
    /// <summary>Prend toute la place disponible sur cet axe (<c>FillWidth</c>, ou un ZStack dont un enfant le fait).</summary>
    public bool FillWidth { get; set; }
    public bool FillHeight { get; set; }
    public float Grow { get; set; }
    /// <summary>Vue qui ne prend aucune place ni d'espacement dans sa pile (barre de menus du système).</summary>
    public bool Collapsed { get; init; }
    public Size Desired { get; set; }
    public Rect Bounds { get; set; }
    public List<LayoutNode> Children { get; } = [];

    public Rect Content => Bounds.Deflate(Padding);
}
