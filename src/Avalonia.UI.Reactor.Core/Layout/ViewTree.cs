using System.Globalization;
using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Rendering;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Layout;

/// <summary>
/// Arbre de vues vivant : layout en deux passes — Measure (taille naturelle, bottom-up) puis
/// Arrange (positions, top-down) — avec recalcul sélectif. Chaque <see cref="ScopeView"/>
/// (composant, élément de ForEach) n'est réévalué que si un état qu'il a lu a changé,
/// et sa mesure est réutilisée telle quelle si rien n'a changé en dessous.
/// </summary>
internal sealed class ViewTree(UiHost host, ITextMeasurer measurer)
{
    readonly Dictionary<string, Scope> _scopes = [];
    readonly Stack<List<Scope>> _collectors = new();
    int _pass, _evaluated, _reused;

    public UpdateStats LastStats { get; private set; }

    public LayoutNode Build(View root, Size viewport, Themes themes)
    {
        _pass++;
        _evaluated = _reused = 0;
        var env = ViewEnvironment.Root(themes);
        var node = Measure(root, env, "r");

        // Les portées qui ont disparu de l'arbre se désabonnent de leurs états.
        foreach (var gone in _scopes.Values.Where(s => s.LastVisit != _pass).ToList())
        {
            gone.Dispose();
            _scopes.Remove(gone.Id);
        }
        LastStats = new UpdateStats(_evaluated, _reused, _scopes.Count);

        LayoutEngine.Arrange(node, new Rect(0, 0, viewport.Width, viewport.Height));
        return node;
    }

    /// <summary>Libère toutes les portées : elles se désabonnent de leurs états (fermeture de la fenêtre).</summary>
    public void Dispose()
    {
        foreach (var scope in _scopes.Values)
        {
            scope.Dispose();
            if (scope.Identity is Component component && ReferenceEquals(component.Host, host)) component.Host = null;
        }
        _scopes.Clear();
    }

    /// <summary>Tout réévaluer au prochain passage (hot reload).</summary>
    public void MarkAllDirty()
    {
        foreach (var s in _scopes.Values) s.MarkDirty();
    }

    /// <summary>Mesure une vue hors de l'arbre (vue produite par le modèle d'un contrôle).</summary>
    public LayoutNode MeasureDetached(View view, ViewEnvironment env, string id) => Measure(view, env, id);

    /// <summary>Environnement de la vue produite par le modèle d'un contrôle : celui du contrôle.</summary>
    internal static ViewEnvironment EnvOf(LayoutNode owner, Themes themes) =>
        ViewEnvironment.Under(themes, owner.Theme, owner.Font, owner.Foreground);

    LayoutNode MeasureScope(ScopeView sv, ViewEnvironment env, string id)
    {
        if (!_scopes.TryGetValue(id, out var scope) || !ReferenceEquals(scope.Identity, sv.Identity))
        {
            scope?.Dispose();
            scope = new Scope(host.RequestUpdate, id, sv.Identity);
            _scopes[id] = scope;
            if (sv.Identity is Component component) component.Host = host; // accès à Themes depuis le composant
        }
        scope.LastVisit = _pass;
        _collectors.TryPeek(out var parent);
        parent?.Add(scope);

        // Un composant garde son contenu tant que son état ne change pas ; un bloc anonyme
        // (ForEach, Observe) est réévalué quand son parent l'a été, car ses paramètres ont pu changer.
        bool reevaluate = scope.IsDirty || scope.Content is null
                          || (sv.Identity is null && !ReferenceEquals(sv, scope.LastView));
        scope.LastView = sv;

        if (!reevaluate && scope.Node is not null && scope.EnvKey is ViewEnvironment cached && cached == env
            && !scope.Descendants.Any(d => d.IsDirty))
        {
            foreach (var d in scope.Descendants) d.LastVisit = _pass;
            parent?.AddRange(scope.Descendants);
            _reused++;
            return scope.Node;
        }

        if (reevaluate)
        {
            scope.Content = scope.Evaluate(sv.Build);
            _evaluated++;
        }

        var inner = new List<Scope>();
        _collectors.Push(inner);
        LayoutNode node;
        try { node = Measure(scope.Content!, env, id + "."); }
        finally { _collectors.Pop(); }

        scope.Descendants = inner;
        parent?.AddRange(inner);
        scope.Node = node;
        scope.EnvKey = env;
        return node;
    }

    LayoutNode Measure(View v, ViewEnvironment env, string id)
    {
        if (v is ScopeView sv)
            return sv.ModifierWrapper() is { } wrapper ? Measure(wrapper, env, id) : MeasureScope(sv, env, id);

        // Groupe non déplié dans une pile (seul, ou avec des modificateurs) : il s'empile verticalement.
        if (v is GroupView group)
            return Measure(Ui.VStack([.. group.Children]).CopyFrom(group), env, id);

        // 1. Thème de la branche (déclaré par la vue, sinon hérité) ; s'il change, on repart de ses valeurs de base.
        env = env.Enter(v);
        var theme = env.Theme;

        // 2. Contrôle avec modèle : sa taille est celle de la vue du modèle (au repos) ; le modèle gère ses marges.
        var template = ControlTemplates.Build(v, theme, hovered: false, pressed: false, focused: false);

        // Barre de menus du système (ou sans menus) : la vue ne prend aucune place, ni d'espacement dans sa pile.
        bool collapsed = v is MenuBarView bar && (host.NativeMenuBar is not null || bar.Menus.Count == 0);
        var menu = theme.Menu;

        var node = new LayoutNode
        {
            View = v,
            Id = id,
            Theme = theme,
            Font = env.FontFor(v),
            Foreground = env.ForegroundFor(v),
            Collapsed = collapsed,
            Padding = collapsed ? default : v.Padding ?? (template is not null ? default : v switch
            {
                ButtonView => theme.Button.Padding,
                TextFieldView => theme.TextField.Padding,
                MenuBarView => menu.BarPadding,
                MenuTitleView => menu.TitlePadding,
                MenuRowView => menu.ItemPadding,
                _ => default,
            }),
            Radius = v.CornerRadius ?? (template is not null ? 0 : v switch
            {
                ButtonView => theme.Button.Radius,
                TextFieldView => theme.TextField.Radius,
                MenuTitleView => menu.TitleRadius,
                MenuRowView => menu.ItemRadius,
                _ => 0,
            }),
        };
        var childEnv = env with { Font = node.Font, Foreground = node.Foreground };

        Size content = template is not null ? Measure(template, childEnv, id + "~").Desired : v switch
        {
            TextView t => measurer.MeasureText(t.Content, node.Font),
            ButtonView b => measurer.MeasureText(b.Label, node.Font),
            TextFieldView or EditorView => new Size(theme.TextField.MinWidth, measurer.MeasureText("Ag", node.Font).Height),
            DividerView => new Size(0, theme.DividerThickness),
            CheckboxView c => MeasureCheckbox(c, node),
            StackView s => MeasureStack(s, node, childEnv),
            ZStackView z => MeasureZStack(z, node, childEnv),
            MenuBarView b => collapsed ? default : MeasureMenuBar(b, node, childEnv),
            MenuTitleView t => measurer.MeasureText(Mnemonics.Parse(t.Menu.Title).Text, node.Font),
            MenuRowView r => MeasureMenuRow(r, node),
            _ => new Size(0, 0),
        };

        float w = content.Width + node.Padding.Horizontal;
        float h = content.Height + node.Padding.Vertical;
        if (v.Width is float fw && !float.IsInfinity(fw) && !collapsed) w = fw;
        if (v.Height is float fh && !float.IsInfinity(fh) && !collapsed) h = fh;
        node.Desired = new Size(w, h);

        // Souplesse : une taille explicite l'emporte ; sinon un conteneur est aussi souple que ses enfants.
        var (fillWidth, fillHeight) = FlexOf(v, node);
        node.FillWidth = v.Width is { } ww ? float.IsPositiveInfinity(ww) : fillWidth;
        node.FillHeight = v.Height is { } hh ? float.IsPositiveInfinity(hh) : fillHeight;
        node.Grow = v.Grow > 0 ? v.Grow : v is ZStackView ? node.Children.Select(c => c.Grow).DefaultIfEmpty(0).Max() : 0;
        return node;
    }

    /// <summary>
    /// Souplesse qu'un conteneur tient de ses enfants, comme dans SwiftUI : une pile qui contient un
    /// <c>Spacer()</c>, un <c>.Grow()</c> ou un <c>.FillWidth()</c> s'étire sans qu'on l'écrive ;
    /// un ZStack (enveloppe d'un composant, <c>Overlay</c>) se comporte comme son contenu.
    /// </summary>
    static (bool Width, bool Height) FlexOf(View v, LayoutNode node) => v switch
    {
        ZStackView => (node.Children.Any(c => c.FillWidth), node.Children.Any(c => c.FillHeight)),
        MenuBarView => (node.Children.Count > 0, false), // la barre occupe toute la largeur
        StackView { Axis: Axis.Horizontal } =>
            (node.Children.Any(c => c.Grow > 0 || c.FillWidth), node.Children.Any(c => c.FillHeight)),
        StackView =>
            (node.Children.Any(c => c.FillWidth), node.Children.Any(c => c.Grow > 0 || c.FillHeight)),
        _ => (false, false),
    };

    Size MeasureZStack(ZStackView z, LayoutNode node, ViewEnvironment env)
    {
        float w = 0, h = 0;
        for (int i = 0; i < z.Children.Count; i++)
        {
            var child = z.Children[i];
            var c = Measure(child, env, $"{node.Id}/{child.Key ?? i.ToString(CultureInfo.InvariantCulture)}");
            node.Children.Add(c);
            w = Math.Max(w, c.Desired.Width);
            h = Math.Max(h, c.Desired.Height);
        }
        return new Size(w, h);
    }

    /// <summary>Titres de la barre dessinée, côte à côte.</summary>
    Size MeasureMenuBar(MenuBarView bar, LayoutNode node, ViewEnvironment env)
    {
        float w = 0, h = 0;
        for (int i = 0; i < bar.Menus.Count; i++)
        {
            var c = Measure(new MenuTitleView(bar, i), env, $"{node.Id}/{i}");
            node.Children.Add(c);
            w += c.Desired.Width;
            h = Math.Max(h, c.Desired.Height);
        }
        return new Size(w, h);
    }

    /// <summary>Colonne de la coche, libellé, puis raccourci (ou flèche de sous-menu) aligné à droite.</summary>
    Size MeasureMenuRow(MenuRowView row, LayoutNode node)
    {
        var m = node.Theme.Menu;
        var label = measurer.MeasureText(Mnemonics.Parse(row.Label).Text, node.Font);
        float w = m.CheckColumnWidth + label.Width;
        if (row.Item is MenuItemView { Gesture: { } gesture })
            w += m.ShortcutSpacing + measurer.MeasureText(gesture.ToString(), node.Font).Width;
        else if (row.HasSubmenu) w += m.ShortcutSpacing + m.IndicatorSize;
        return new Size(w, Math.Max(label.Height, m.IndicatorSize));
    }

    Size MeasureCheckbox(CheckboxView c, LayoutNode node)
    {
        var ct = node.Theme.Checkbox;
        if (c.Label is null) return new Size(ct.Size, ct.Size);
        var t = measurer.MeasureText(c.Label, node.Font);
        return new Size(ct.Size + ct.LabelSpacing + t.Width, Math.Max(ct.Size, t.Height));
    }

    Size MeasureStack(StackView s, LayoutNode node, ViewEnvironment env)
    {
        bool vertical = s.Axis == Axis.Vertical;
        float main = 0, cross = 0;
        for (int i = 0; i < s.Children.Count; i++)
        {
            var child = s.Children[i];
            var c = Measure(child, env, $"{node.Id}/{child.Key ?? i.ToString(CultureInfo.InvariantCulture)}");
            node.Children.Add(c);
            main += vertical ? c.Desired.Height : c.Desired.Width;
            cross = Math.Max(cross, vertical ? c.Desired.Width : c.Desired.Height);
        }
        int shown = node.Children.Count(c => !c.Collapsed);
        if (shown > 1) main += (s.Spacing ?? node.Theme.Spacing) * (shown - 1);
        return vertical ? new Size(cross, main) : new Size(main, cross);
    }
}
