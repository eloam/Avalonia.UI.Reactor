using Mvu.Primitives;
using Mvu.State;
using Mvu.Theming;

namespace Mvu.Views;

/// <summary>
/// Les mêmes modificateurs sur un composant : <c>counter.Padding(8).Background(ColorRole.Surface)</c>.
/// Ils s'appliquent autour de son contenu ; le composant garde son état et son recalcul indépendant.
/// </summary>
public static class ComponentModifiers
{
    static View V(Component c) => c;

    public static View Key(this Component c, string key) => V(c).Key(key);
    public static View Padding(this Component c, float all) => V(c).Padding(all);
    public static View Padding(this Component c, float horizontal, float vertical) => V(c).Padding(horizontal, vertical);
    public static View Padding(this Component c, float left, float top, float right, float bottom) =>
        V(c).Padding(left, top, right, bottom);
    public static View Width(this Component c, float width) => V(c).Width(width);
    public static View Height(this Component c, float height) => V(c).Height(height);
    public static View Size(this Component c, float width, float height) => V(c).Size(width, height);
    public static View FillWidth(this Component c) => V(c).FillWidth();
    public static View FillHeight(this Component c) => V(c).FillHeight();
    public static View Grow(this Component c, float factor = 1) => V(c).Grow(factor);
    public static View AlignSelf(this Component c, Align align) => V(c).AlignSelf(align);
    public static View Background(this Component c, ColorValue color) => V(c).Background(color);
    public static View Border(this Component c, ColorValue color, float width = 1) => V(c).Border(color, width);
    public static View Radius(this Component c, float radius) => V(c).Radius(radius);
    public static View Foreground(this Component c, ColorValue color) => V(c).Foreground(color);
    public static View FontSize(this Component c, float size) => V(c).FontSize(size);
    public static View Bold(this Component c, bool bold = true) => V(c).Bold(bold);
    public static View Font(this Component c, TextStyle style) => V(c).Font(style);
    public static View Font(this Component c, string styleName) => V(c).Font(styleName);
    public static View Theme(this Component c, string? name) => V(c).Theme(name);
    public static View Theme(this Component c, ThemeFamily family) => V(c).Theme(family);
    public static View Theme(this Component c, Theme theme) => V(c).Theme(theme);
    public static View ColorScheme(this Component c, ColorScheme? scheme) => V(c).ColorScheme(scheme);
    public static View OnTap(this Component c, Action action) => V(c).OnTap(action);
    public static ZStackView Overlay(this Component c, View overlay, Alignment? alignment = null) => V(c).Overlay(overlay, alignment);
}
