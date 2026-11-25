// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using MartletUI.State;
using MartletUI.Theming;
using MartletUI.Views;

namespace MartletUI.Reconciliation;

/// <summary>
/// Applies a tree of views (immutable records) to a tree of Avalonia controls that it keeps from one update to the next.
/// </summary>
/// <remarks>
/// <para>
/// A control is reused as long as its view keeps the same place (<see cref="ChildSlots"/>) and the same kind of control:
/// only its properties change, so it keeps its focus, hover and selection. Each scope (component, view method called in
/// a block, <c>ForEach</c> item, <c>Observe</c> block) is re-evaluated only when some state it read changed; otherwise
/// its controls stay as they are.
/// </para>
/// <para>
/// The reconciler walks the views and the scopes; the rest is delegated: one adapter per type of view
/// (<see cref="ViewAdapter"/>), the decorations (<see cref="Decorations"/>), the size and flexibility
/// (<see cref="Placement"/>), the lifecycles (<see cref="LifecycleTracker"/>) and the moves of controls
/// (<see cref="ControlTree"/>).
/// </para>
/// </remarks>
/// <param name="host">The host that displays the tree.</param>
/// <param name="run">The function that runs the action of a control, then updates the interface.</param>
internal sealed class Reconciler(IScopeHost host, Action<Action?> run)
{
    // The path of the root view.
    private const string RootId = "r";

    // The suffix of the path of the content of a scope, below the path of the scope.
    private const string ScopeContentSuffix = ".";

    // The suffix of the path of the view of a template, below the path of its control.
    private const string TemplateSuffix = "~";

    private readonly IScopeHost _host = host;
    private readonly Action<Action?> _run = run;
    private readonly LifecycleTracker _lifecycles = new(host, run);
    private readonly Dictionary<string, Scope> _scopes = [];
    private readonly Dictionary<Scope, IReadOnlyList<Node>> _content = [];
    private readonly Stack<List<Scope>> _collectors = new();
    private readonly Stack<Scope> _owners = new();
    private Node? _root;
    private int _pass;
    private int _depth;
    private int _evaluated;
    private int _reused;

    /// <summary>
    /// Gets the outcome of the last update: scopes re-evaluated, scopes kept, total.
    /// </summary>
    public UpdateStats LastStats { get; private set; }

    /// <summary>
    /// Gets or sets the node of the menu bar displayed in the window, whose shortcuts the host handles, or
    /// <see langword="null"/> if there is none.
    /// </summary>
    public Node? MenuBar { get; set; }

    /// <summary>
    /// Gets the theme of the root view at the last update, which gives the window its background.
    /// </summary>
    public Theme? RootTheme => _root?.Environment.Theme;

    /// <summary>
    /// Sets properties on the control of a node, so that the events they raise (text, check state) are known to come from
    /// the view and do not go back to the state.
    /// </summary>
    /// <remarks>
    /// This is also how a controlled control (field, checkbox) takes back the value of its view after its action, when the
    /// state did not follow (no action, rejected input): no scope is re-evaluated then.
    /// </remarks>
    /// <param name="node">The node whose control is updated.</param>
    /// <param name="set">The function that sets the properties.</param>
    public static void Apply(Node node, Action set)
    {
        node.Applying = true;
        try
        {
            set();
        }
        finally
        {
            node.Applying = false;
        }
    }

    /// <summary>
    /// Runs the action of a control (a click), then updates the interface.
    /// </summary>
    /// <param name="action">The action, or <see langword="null"/>.</param>
    public void Run(Action? action) => _run(action);

    /// <summary>
    /// Applies the root view.
    /// </summary>
    /// <param name="root">The root view.</param>
    /// <param name="themes">The theme catalog.</param>
    /// <returns>The control to place in the window.</returns>
    public Control Update(View root, Themes themes)
    {
        _pass++;
        _evaluated = 0;
        _reused = 0;
        _depth = 0;
        _root = Reconcile(_root, root, ViewEnvironment.Root(themes), RootId);

        // The scopes that left the tree unsubscribe from their state.
        foreach (Scope gone in _scopes.Values.Where(s => s.LastVisit != _pass).ToList())
        {
            Release(gone);
            _scopes.Remove(gone.Id);
        }

        _lifecycles.Sweep(_pass, IsKept);
        LastStats = new UpdateStats(_evaluated, _reused, _scopes.Count);
        return _root.Root;
    }

    /// <summary>
    /// Releases every scope when the window closes, and runs the <c>OnDisappear</c> actions of the views that appeared;
    /// the next update starts from scratch.
    /// </summary>
    public void Release()
    {
        _lifecycles.Release();
        foreach (Scope scope in _scopes.Values)
        {
            Release(scope);
        }

        _scopes.Clear();
        _root = null;
    }

    /// <summary>
    /// Marks every scope for re-evaluation at the next update, after a hot reload.
    /// </summary>
    public void MarkAllDirty()
    {
        foreach (Scope scope in _scopes.Values)
        {
            scope.MarkDirty();
        }
    }

    /// <summary>
    /// Applies a view at a path, reusing the node that held it when it fits.
    /// </summary>
    /// <param name="existing">The node that held the path, or <see langword="null"/>.</param>
    /// <param name="view">The view to apply.</param>
    /// <param name="environment">The environment inherited from the parent.</param>
    /// <param name="id">The path of the view.</param>
    /// <returns>The node of the view.</returns>
    public Node Reconcile(Node? existing, View view, ViewEnvironment environment, string id)
    {
        if (view is ScopeView scope)
        {
            return scope.ModifierWrapper() is { } wrapper
                ? Reconcile(existing, wrapper, environment, id)
                : ReconcileScope(scope, environment, id);
        }

        // A group that no container unfolded (alone, as the content of a component or a row) stacks its children
        // vertically. Its modifiers go to each child, as in a container; its key identifies the stack.
        if (view is GroupView group)
        {
            view = Ui.VStack([.. Ui.Unfold(group)]) with { Key = group.Key };
        }

        environment = environment.Enter(view);
        var adapter = ViewAdapter.For(view);
        Node node = existing is not null && existing.Adapter == adapter && existing.Id == id && adapter.CanReuse(existing, view)
            ? existing
            : new Node(id, adapter, view, n => adapter.Create(this, n));
        node.View = view;
        node.Environment = environment;
        _lifecycles.Track(node, view, _owners.TryPeek(out Scope? owner) ? owner : null, _pass, _depth);
        _depth++;
        Apply(node, () => adapter.Update(this, node, view, environment));
        _depth--;
        Decorations.Decorate(node, view, environment.Theme, Run);
        Placement.Place(node, view);
        return node;
    }

    /// <summary>
    /// Applies the view produced by the template of a control (its look, recomputed when its state changes), as a
    /// subtree of the control, in the environment of the control (its theme, font and color), reusing the controls of
    /// the previous version.
    /// </summary>
    /// <param name="owner">The node of the templated control.</param>
    /// <param name="template">The view produced by the template.</param>
    /// <returns>The control to display in the templated control.</returns>
    public Control RenderTemplate(Node owner, View template)
    {
        ViewEnvironment environment = owner.Environment with
        {
            Font = owner.Environment.FontFor(owner.View),
            Foreground = owner.Environment.ForegroundFor(owner.View),
        };
        owner.Template = Reconcile(owner.Template, template, environment, owner.Id + TemplateSuffix);
        return owner.Template.Root;
    }

    /// <summary>
    /// Applies the children of a container: each one reuses the node that held its place (see <see cref="ChildSlots"/>),
    /// which keeps its control, focus and hover when a sibling appears, disappears or moves.
    /// </summary>
    /// <param name="parent">The node of the container.</param>
    /// <param name="children">The child views.</param>
    /// <param name="environment">The environment of the container.</param>
    /// <returns>The nodes of the children, in order.</returns>
    /// <exception cref="InvalidOperationException">Two children have the same key.</exception>
    public IReadOnlyList<Node> ReconcileChildren(Node parent, IReadOnlyList<View> children, ViewEnvironment environment)
    {
        var previous = new Dictionary<string, Node>(parent.Children.Count);
        foreach (Node child in parent.Children)
        {
            previous.TryAdd(child.Slot, child);
        }

        var slots = new ChildSlots(parent.Id);
        var result = new List<Node>(children.Count);
        foreach (View view in children)
        {
            string slot = slots.Of(view);
            if (view is ScopeView scope)
            {
                result.AddRange(ReconcileFragment(scope, environment, slot));
            }
            else
            {
                result.Add(ReconcileAt(previous, view, environment, slot));
            }
        }

        parent.Children = result;
        return result;
    }

    // A scope in a place that holds one control (the root, a decoration, a template): a content made of several views is
    // a group, which stacks them vertically.
    private Node ReconcileScope(ScopeView view, ViewEnvironment environment, string id) =>
        ReconcileScopeContent(view, environment, id, (scope, previous) =>
            [Reconcile(previous is { Count: > 0 } ? previous[0] : null, scope.Content!, environment, id + ScopeContentSuffix)])[0];

    // A scope among the children of a container is a fragment: when its content is a group, its views take their places
    // among the children of the container, below the place of the scope, with the modifiers of the scope (as for a
    // group). A scope whose content is one view keeps its modifiers around it. Updating a fragment alone needs nothing
    // more: while one of its scopes is dirty, its parents are visited, and the container arranges its children again.
    private IReadOnlyList<Node> ReconcileFragment(ScopeView view, ViewEnvironment environment, string slot) =>
        ReconcileScopeContent(view, environment, slot, (scope, previous) =>
            FragmentNodes(view, scope.Content!, environment, slot, previous));

    private List<Node> FragmentNodes(
        ScopeView view, View content, ViewEnvironment environment, string slot, IReadOnlyList<Node>? previousNodes)
    {
        var previous = new Dictionary<string, Node>();
        foreach (Node node in previousNodes ?? [])
        {
            previous.TryAdd(node.Slot, node);
        }

        ScopeView modifiers = view with { Key = null };
        bool modified = !modifiers.HasNoModifiers();
        var nodes = new List<Node>();
        if (content is GroupView group)
        {
            var slots = new ChildSlots(slot + ScopeContentSuffix);
            int position = 0;
            foreach (View child in Ui.Unfold(group))
            {
                View placed = modified ? child.Inherit(modifiers, position) : child;
                position++;
                string childSlot = slots.Of(placed);
                if (placed is ScopeView nested)
                {
                    nodes.AddRange(ReconcileFragment(nested, environment, childSlot));
                }
                else
                {
                    nodes.Add(ReconcileAt(previous, placed, environment, childSlot));
                }
            }
        }
        else
        {
            View single = modified
                ? new ZStackView([content]) { Alignment = Primitives.Alignment.TopLeft }.CopyFrom(modifiers)
                : content;
            previous.TryGetValue(slot, out Node? existing);
            Node node = Reconcile(existing, single, environment, slot + ScopeContentSuffix);
            node.Slot = slot;
            nodes.Add(node);
        }

        return nodes;
    }

    // Applies a view at its place among the children of a container, reusing the node that held that place.
    private Node ReconcileAt(Dictionary<string, Node> previous, View view, ViewEnvironment environment, string slot)
    {
        previous.TryGetValue(slot, out Node? existing);
        Node node = Reconcile(existing, view, environment, slot);
        node.Slot = slot;
        return node;
    }

    // The steps shared by a scope in one place and by a fragment: find or create the scope at its place, re-evaluate its
    // Body when needed, keep its nodes when nothing changed, otherwise reconcile its content while collecting its
    // descendant scopes and owning the lifecycles declared in it.
    private IReadOnlyList<Node> ReconcileScopeContent(
        ScopeView view, ViewEnvironment environment, string id, Func<Scope, IReadOnlyList<Node>?, IReadOnlyList<Node>> reconcile)
    {
        if (!_scopes.TryGetValue(id, out Scope? scope) || !ReferenceEquals(scope.Identity, view.Identity))
        {
            if (scope is not null)
            {
                Release(scope);
            }

            scope = new Scope(_host.RequestUpdate, id, view.Identity);
            _scopes[id] = scope;
            if (view.Identity is Component component)
            {
                // Gives the component access to the theme catalog (Component.Themes).
                component.Host = _host;
            }
        }

        scope.LastVisit = _pass;
        _collectors.TryPeek(out List<Scope>? parent);
        parent?.Add(scope);

        bool reevaluate = scope.IsDirty || scope.Content is null || InputsChanged(view, scope.LastView);
        scope.LastView = view;
        _content.TryGetValue(scope, out IReadOnlyList<Node>? previous);

        if (!reevaluate
            && previous is not null
            && scope.Environment is { } cached
            && cached == environment
            && !scope.Descendants.Any(d => d.IsDirty))
        {
            foreach (Scope descendant in scope.Descendants)
            {
                descendant.LastVisit = _pass;
            }

            parent?.AddRange(scope.Descendants);
            _reused++;
            return previous;
        }

        if (reevaluate)
        {
            scope.Content = scope.Evaluate(() => Build(view));
            scope.EvaluatedPass = _pass;
            _evaluated++;
        }

        var inner = new List<Scope>();
        _collectors.Push(inner);
        _owners.Push(scope);
        IReadOnlyList<Node> nodes;
        try
        {
            nodes = reconcile(scope, previous);
        }
        finally
        {
            _collectors.Pop();
            _owners.Pop();
        }

        scope.Descendants = inner;
        parent?.AddRange(inner);
        scope.Environment = environment;
        _content[scope] = nodes;
        return nodes;
    }

    // A component keeps its content as long as its state does not change; a view method as long as its arguments are
    // equal; an anonymous block (ForEach, Observe) is re-evaluated when its parent was, because its parameters may have
    // changed.
    private static bool InputsChanged(ScopeView view, ScopeView? last) => view switch
    {
        { Identity: Component } => false,
        { Inputs: { } inputs } => !Equals(inputs, last?.Inputs),
        _ => !ReferenceEquals(view, last),
    };

    // A view method also follows Refresh() of the component that calls it: skipped when its arguments are equal, it
    // would otherwise miss a plain field that Refresh() announces.
    private View Build(ScopeView view)
    {
        if (view.Inputs is not null && _owners.Select(owner => owner.Identity).OfType<Component>().FirstOrDefault() is { } component)
        {
            component.TrackRefresh();
        }

        return view.Build();
    }

    private void Release(Scope scope)
    {
        scope.Release();
        _content.Remove(scope);
        if (scope.Identity is Component component && ReferenceEquals(component.Host, _host))
        {
            component.Host = null;
        }
    }

    // Kept by the update: visited, not re-evaluated, and still the scope at its place.
    private bool IsKept(Scope? owner) =>
        owner is not null
        && owner.LastVisit == _pass
        && owner.EvaluatedPass != _pass
        && _scopes.TryGetValue(owner.Id, out Scope? live)
        && ReferenceEquals(live, owner);
}
