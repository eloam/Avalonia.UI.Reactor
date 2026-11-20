using System.Globalization;
using Avalonia.Collections;
using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Applique un arbre de vues (records immuables) à un arbre de contrôles Avalonia qu'il garde d'une mise à jour à
/// l'autre. Un contrôle est repris tant que sa vue garde le même chemin (son <c>Key</c>, sinon son rang) et la même
/// sorte de contrôle : seules ses propriétés changent, il garde focus, survol et sélection. Chaque portée (composant,
/// <c>ForEach</c>, <c>Observe</c>) n'est réévaluée que si un état qu'elle a lu a changé ; sinon ses contrôles restent tels quels.
/// </summary>
internal sealed class Reconciler(IScopeHost host, Action<Action?> run)
{
    readonly Dictionary<string, Scope> _scopes = [];
    readonly Dictionary<Scope, Node> _content = [];
    readonly Stack<List<Scope>> _collectors = new();
    Node? _root;
    int _pass, _evaluated, _reused;

    /// <summary>Bilan de la dernière mise à jour : portées réévaluées, reprises telles quelles, total.</summary>
    public UpdateStats LastStats { get; private set; }

    /// <summary>Thème de la vue racine à la dernière mise à jour (fond de la fenêtre).</summary>
    public Theme? RootTheme => _root?.Environment.Theme;

    /// <summary>Exécute l'action d'un contrôle (un clic), puis met l'interface à jour.</summary>
    public void Run(Action? action) => run(action);

    /// <summary>Applique la vue racine ; renvoie le contrôle à placer dans la fenêtre.</summary>
    public Control Update(View root, Themes themes)
    {
        _pass++;
        _evaluated = _reused = 0;
        _root = Reconcile(_root, root, ViewEnvironment.Root(themes), "r");

        // Les portées qui ont disparu de l'arbre se désabonnent de leurs états.
        foreach (var gone in _scopes.Values.Where(s => s.LastVisit != _pass).ToList())
        {
            Release(gone);
            _scopes.Remove(gone.Id);
        }
        LastStats = new UpdateStats(_evaluated, _reused, _scopes.Count);
        return _root.Root;
    }

    /// <summary>Libère toutes les portées (fenêtre fermée) ; la mise à jour suivante repartira de zéro.</summary>
    public void Dispose()
    {
        foreach (var scope in _scopes.Values) Release(scope);
        _scopes.Clear();
        _root = null;
    }

    /// <summary>Applique une vue à la place <paramref name="id"/>, en reprenant le nœud qui l'occupait s'il convient.</summary>
    internal Node Reconcile(Node? existing, View view, ViewEnvironment env, string id)
    {
        if (view is ScopeView scope)
            return scope.ModifierWrapper() is { } wrapper ? Reconcile(existing, wrapper, env, id) : ReconcileScope(scope, env, id);

        // Groupe non déplié dans une pile (seul, ou avec des modificateurs) : il s'empile verticalement.
        if (view is GroupView group) view = Ui.VStack([.. group.Children]).CopyFrom(group);

        env = env.Enter(view);
        var adapter = ViewAdapter.For(view);
        var node = existing is not null && existing.Adapter == adapter && existing.Id == id
            ? existing
            : new Node(id, adapter, view, n => adapter.Create(this, n));
        node.View = view;
        node.Environment = env;
        adapter.Update(this, node, view, env);
        if (!adapter.DrawsDecoration) Decorate(node, view, env.Theme);
        Place(node, view);
        return node;
    }

    /// <summary>
    /// Enfants d'un conteneur : chacun reprend le nœud qui occupait sa place (<c>Key</c> de la vue, sinon son rang),
    /// ce qui garde les contrôles d'une liste quand ses éléments changent d'ordre.
    /// </summary>
    internal IReadOnlyList<Node> ReconcileChildren(Node parent, IReadOnlyList<View> children, ViewEnvironment env)
    {
        var previous = new Dictionary<string, Node>(parent.Children.Count);
        foreach (var child in parent.Children) previous.TryAdd(child.Slot, child);

        var slots = new HashSet<string>();
        var result = new Node[children.Count];
        for (int i = 0; i < children.Count; i++)
        {
            var view = children[i];
            string slot = $"{parent.Id}/{view.Key ?? i.ToString(CultureInfo.InvariantCulture)}";
            if (!slots.Add(slot))
                throw new InvalidOperationException(
                    $"Two children of the same container have the key '{view.Key}'. Give each child a unique key.");
            previous.TryGetValue(slot, out var existing);
            var node = Reconcile(existing, view, env, slot);
            node.Slot = slot;
            result[i] = node;
        }
        parent.Children = result;
        return result;
    }

    /// <summary>
    /// Range les contrôles des enfants dans le conteneur avec le moins de changements possible : un contrôle déjà
    /// présent est déplacé, pas retiré puis remis, pour qu'il garde son focus et son survol.
    /// </summary>
    internal static void Arrange(IAvaloniaList<Control> children, IReadOnlyList<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var wanted = nodes[i].Root;
            if (i < children.Count && ReferenceEquals(children[i], wanted)) continue;
            int at = children.IndexOf(wanted);
            if (at > i) children.Move(at, i);
            else
            {
                Detach(wanted);
                children.Insert(i, wanted);
            }
        }
        while (children.Count > nodes.Count) children.RemoveAt(children.Count - 1);
    }

    /// <summary>Retire un contrôle de son parent actuel, avant de le placer ailleurs.</summary>
    internal static void Detach(Control control)
    {
        switch (control.Parent)
        {
            case Panel panel: panel.Children.Remove(control); break;
            case Decorator decorator: decorator.Child = null; break;
        }
    }

    Node ReconcileScope(ScopeView view, ViewEnvironment env, string id)
    {
        if (!_scopes.TryGetValue(id, out var scope) || !ReferenceEquals(scope.Identity, view.Identity))
        {
            if (scope is not null) Release(scope);
            scope = new Scope(host, id, view.Identity);
            _scopes[id] = scope;
            if (view.Identity is Component component) component.Host = host; // accès à Themes depuis le composant
        }
        scope.LastVisit = _pass;
        _collectors.TryPeek(out var parent);
        parent?.Add(scope);

        // Un composant garde son contenu tant que son état ne change pas ; un bloc anonyme (ForEach, Observe)
        // est réévalué quand son parent l'a été, car ses paramètres ont pu changer.
        bool reevaluate = scope.IsDirty || scope.Content is null
                          || (view.Identity is null && !ReferenceEquals(view, scope.LastView));
        scope.LastView = view;
        _content.TryGetValue(scope, out var previous);

        if (!reevaluate && previous is not null && scope.EnvKey is ViewEnvironment cached && cached == env
            && !scope.Descendants.Any(d => d.IsDirty))
        {
            foreach (var d in scope.Descendants) d.LastVisit = _pass;
            parent?.AddRange(scope.Descendants);
            _reused++;
            return previous;
        }

        if (reevaluate)
        {
            scope.Content = scope.Evaluate(view.Build);
            _evaluated++;
        }

        var inner = new List<Scope>();
        _collectors.Push(inner);
        Node node;
        try { node = Reconcile(previous, scope.Content!, env, id + "."); }
        finally { _collectors.Pop(); }

        scope.Descendants = inner;
        parent?.AddRange(inner);
        scope.EnvKey = env;
        _content[scope] = node;
        return node;
    }

    void Release(Scope scope)
    {
        scope.Dispose();
        _content.Remove(scope);
        if (scope.Identity is Component component && ReferenceEquals(component.Host, host)) component.Host = null;
    }

    /// <summary>
    /// Fond, bordure, angles et marge intérieure d'une vue dont le contrôle ne les dessine pas (texte, pile, espace) :
    /// une enveloppe <see cref="Border"/> les porte, posée seulement quand la vue en déclare.
    /// </summary>
    static void Decorate(Node node, View view, Theme theme)
    {
        bool needed = view.Background is not null || view.BorderColor is not null
                      || view.CornerRadius is not null || view.Padding is not null;
        if (needed && node.Decoration is null)
        {
            Detach(node.Content);
            node.Decoration = new Border { Child = node.Content };
        }
        else if (!needed && node.Decoration is { } unused)
        {
            unused.Child = null;
            node.Decoration = null;
        }

        if (node.Decoration is not { } border) return;
        border.SetBrush(Border.BackgroundProperty, view.Background?.Resolve(theme.Palette));
        border.SetBrush(Border.BorderBrushProperty, view.BorderColor?.Resolve(theme.Palette));
        border.BorderThickness = new Thickness(view.BorderColor is null ? 0 : view.BorderWidth ?? 1);
        border.CornerRadius = new CornerRadius(view.CornerRadius ?? 0);
        border.Padding = (view.Padding ?? default).ToAvalonia();
    }

    /// <summary>
    /// Taille et souplesse : une taille explicite l'emporte (<c>.Width(72)</c>, ou +∞ pour remplir) ; sinon un conteneur
    /// est aussi souple que ses enfants. La pile parente lit ces valeurs pour répartir sa place.
    /// </summary>
    static void Place(Node node, View view)
    {
        var (fillWidth, fillHeight) = node.Adapter.FlexOf(node, view);
        node.FillWidth = view.Width is { } width ? float.IsPositiveInfinity(width) : fillWidth;
        node.FillHeight = view.Height is { } height ? float.IsPositiveInfinity(height) : fillHeight;
        node.Grow = view.Grow;

        var root = node.Root;
        root.Width = view.Width is { } w && !float.IsInfinity(w) ? w : double.NaN;
        root.Height = view.Height is { } h && !float.IsInfinity(h) ? h : double.NaN;
        if (node.Decoration is not null)
        {
            node.Content.Width = double.NaN;
            node.Content.Height = double.NaN;
        }
        Panels.FlexStack.SetGrow(root, node.Grow);
        Panels.FlexStack.SetFillWidth(root, node.FillWidth);
        Panels.FlexStack.SetFillHeight(root, node.FillHeight);
        Panels.FlexStack.SetSelfAlign(root, view.SelfAlign);
    }
}
