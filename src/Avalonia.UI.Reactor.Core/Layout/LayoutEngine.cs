using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Layout;

internal static class LayoutEngine
{
    internal static void Arrange(LayoutNode node, Rect rect)
    {
        node.Bounds = rect;
        if (node.Children.Count == 0) return;
        if (node.View is ZStackView z) { ArrangeZStack(node, z); return; }
        if (node.View is MenuBarView) { ArrangeRow(node); return; }
        if (node.View is not StackView s) return;

        var content = node.Content;
        bool vertical = s.Axis == Axis.Vertical;
        float mainAvail = vertical ? content.Height : content.Width;
        float crossAvail = vertical ? content.Width : content.Height;

        float fixedSum = 0, growSum = 0;
        foreach (var c in node.Children)
        {
            fixedSum += vertical ? c.Desired.Height : c.Desired.Width;
            growSum += GrowOf(c, vertical);
        }
        float gap = s.Spacing ?? node.Theme.Spacing;
        float spacing = gap * Math.Max(0, node.Children.Count(c => !c.Collapsed) - 1);
        float remaining = Math.Max(0, mainAvail - fixedSum - spacing);

        float pos = vertical ? content.Y : content.X;
        foreach (var c in node.Children)
        {
            float grow = GrowOf(c, vertical);
            float main = (vertical ? c.Desired.Height : c.Desired.Width) + (grow > 0 ? remaining * grow / growSum : 0);

            var align = c.View.SelfAlign ?? s.CrossAlign;
            bool fillCross = align == Align.Stretch || (vertical ? c.FillWidth : c.FillHeight);
            float natural = vertical ? c.Desired.Width : c.Desired.Height;
            float cross = fillCross ? crossAvail : Math.Min(natural, crossAvail);
            float offset = align switch
            {
                Align.Center => (crossAvail - cross) / 2,
                Align.End => crossAvail - cross,
                _ => 0,
            };

            var r = vertical
                ? new Rect(content.X + offset, pos, cross, main)
                : new Rect(pos, content.Y + offset, main, cross);
            Arrange(c, r);
            pos += main + (c.Collapsed ? 0 : gap); // une vue repliée ne crée pas d'espacement
        }
    }

    /// <summary>Titres de la barre de menus : côte à côte, à leur largeur, sur toute la hauteur.</summary>
    static void ArrangeRow(LayoutNode node)
    {
        var content = node.Content;
        float x = content.X;
        foreach (var c in node.Children)
        {
            Arrange(c, new Rect(x, content.Y, c.Desired.Width, content.Height));
            x += c.Desired.Width;
        }
    }

    static float GrowOf(LayoutNode n, bool vertical) =>
        n.Grow > 0 ? n.Grow : (vertical ? n.FillHeight : n.FillWidth) ? 1 : 0;

    static void ArrangeZStack(LayoutNode node, ZStackView z)
    {
        var content = node.Content;
        foreach (var c in node.Children)
        {
            var (x, w) = Place(content.X, content.Width, c.Desired.Width, c.FillWidth, c.View.SelfAlign ?? z.Alignment.Horizontal);
            var (y, h) = Place(content.Y, content.Height, c.Desired.Height, c.FillHeight, z.Alignment.Vertical);
            Arrange(c, new Rect(x, y, w, h));
        }
    }

    static (float Pos, float Size) Place(float start, float avail, float natural, bool fill, Align align)
    {
        float size = fill || align == Align.Stretch ? avail : Math.Min(natural, avail);
        float pos = align switch
        {
            Align.Center => start + (avail - size) / 2,
            Align.End => start + avail - size,
            _ => start,
        };
        return (pos, size);
    }

    public static LayoutNode? HitTest(LayoutNode node, Point p)
    {
        if (!node.Bounds.Contains(p)) return null;
        for (int i = node.Children.Count - 1; i >= 0; i--)
            if (HitTest(node.Children[i], p) is { } hit) return hit;
        return IsInteractive(node.View) ? node : null;
    }

    public static bool IsInteractive(View v) =>
        v is ButtonView or TextFieldView or CheckboxView or MenuTitleView or MenuRowView || v.TapAction is not null;

    public static IEnumerable<LayoutNode> Flatten(LayoutNode node)
    {
        yield return node;
        foreach (var c in node.Children)
            foreach (var n in Flatten(c)) yield return n;
    }
}
