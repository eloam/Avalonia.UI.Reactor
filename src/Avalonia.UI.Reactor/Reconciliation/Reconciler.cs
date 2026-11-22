// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Globalization;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Applies a tree of views (immutable records) to a tree of Avalonia controls that it keeps from one update to the next.
/// </summary>
/// <remarks>
/// A control is reused as long as its view keeps the same place (see <see cref="ReconcileChildren"/>) and the same kind
/// of control: only its properties change, so it keeps its focus, hover and selection. Each scope (component,
/// <c>ForEach</c> item, <c>Observe</c> block) is re-evaluated only when some state it read changed; otherwise its controls
/// stay as they are. A view that declares <c>OnAppear</c> or <c>OnDisappear</c> has a lifecycle, which follows its node.
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

    // The prefixes of the place of a child below its container, one per rule (see SlotOf): two rules never give the same
    // place, even when a key looks like a rank (a row keyed by the id 0, next to an unkeyed header).
    private const string KeySlotPrefix = "k";
    private const string IdentitySlotPrefix = "o";
    private const string RankSlotPrefix = "t";

    // Separates the type of a view from its rank in a place given by rule 3, and the owner of a property from its name.
    private const char RankSeparator = '#';
    private const char PropertySeparator = '.';

    private readonly Dictionary<string, Scope> _scopes = [];
    private readonly Dictionary<Scope, IReadOnlyList<Node>> _content = [];
    private readonly Stack<List<Scope>> _collectors = new();
    private readonly HashSet<Lifecycle> _lifecycles = [];
    private readonly Stack<Scope> _owners = new();
    private Node? _root;
    private int _pass;
    private int _depth;
    private long _visits;
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
    /// Removes a control from its current parent, before placing it elsewhere.
    /// </summary>
    /// <param name="control">The control to remove.</param>
    public static void Detach(Control control)
    {
        switch (control.Parent)
        {
            case Panel panel:
                panel.Children.Remove(control);
                break;
            case Decorator decorator:
                decorator.Child = null;
                break;
        }
    }

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
    /// Arranges the controls of the children in a container with as few changes as possible: a control already present
    /// is moved, not removed and inserted again, so that it keeps its focus and hover.
    /// </summary>
    /// <param name="children">The children of the container.</param>
    /// <param name="nodes">The nodes of the children, in order.</param>
    public static void Arrange(IAvaloniaList<Control> children, IReadOnlyList<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Control wanted = nodes[i].Root;
            if (i < children.Count && ReferenceEquals(children[i], wanted))
            {
                continue;
            }

            int index = children.IndexOf(wanted);
            if (index > i)
            {
                children.Move(index, i);
            }
            else
            {
                Detach(wanted);
                children.Insert(i, wanted);
            }
        }

        while (children.Count > nodes.Count)
        {
            children.RemoveAt(children.Count - 1);
        }
    }

    /// <summary>
    /// Sets the background, border, corners, padding and click of a view on the surface that carries them.
    /// </summary>
    /// <param name="surface">The surface.</param>
    /// <param name="view">The view.</param>
    /// <param name="theme">The theme of the view.</param>
    public static void ApplyDecoration(Surface surface, View view, Theme theme)
    {
        surface.Paint(view.Background?.Resolve(theme.Palette), theme, tappable: view.TapAction is not null);
        surface.SetBrush(Border.BorderBrushProperty, view.BorderColor?.Resolve(theme.Palette));
        surface.BorderThickness = new Thickness(view.BorderColor is null ? 0 : view.BorderWidth ?? 1);
        surface.CornerRadius = new CornerRadius(view.CornerRadius ?? 0);
        surface.Padding = (view.Padding ?? default).ToAvalonia();
    }

    /// <summary>
    /// Runs the action of a control (a click), then updates the interface.
    /// </summary>
    /// <param name="action">The action, or <see langword="null"/>.</param>
    public void Run(Action? action) => run(action);

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

        SweepLifecycles();
        LastStats = new UpdateStats(_evaluated, _reused, _scopes.Count);
        return _root.Root;
    }

    /// <summary>
    /// Releases every scope when the window closes, and runs the <c>OnDisappear</c> actions of the views that appeared;
    /// the next update starts from scratch.
    /// </summary>
    public void Release()
    {
        ReleaseLifecycles();
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
        TrackLifecycle(node, view);
        _depth++;
        Apply(node, () => adapter.Update(this, node, view, environment));
        _depth--;
        Decorate(node, view, environment.Theme);
        Place(node, view);
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
    /// Applies the children of a container: each one reuses the node that held its place, which keeps its control, focus
    /// and hover when a sibling appears, disappears or moves.
    /// </summary>
    /// <remarks>
    /// The place of a child comes from, in this order: its <c>Key</c>; an identity it already has (a component by its
    /// instance, a bound control by the property of its binding); otherwise its view type and its rank among the
    /// children of that type, so that a view of another type that appears before it does not shift it.
    /// </remarks>
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

        var slots = new HashSet<string>();
        var ranks = new Dictionary<Type, int>();
        var result = new List<Node>(children.Count);
        foreach (View view in children)
        {
            string slot = SlotOf(parent.Id, view, slots, ranks);
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

    // The place of a child (see ReconcileChildren). Two children that share an identity (the same component shown twice,
    // two fields bound to the same property) fall back to their rank; two children that share a key are an error.
    private static string SlotOf(string parentId, View view, HashSet<string> slots, Dictionary<Type, int> ranks)
    {
        if (view.Key is { } key)
        {
            string keyed = $"{parentId}/{KeySlotPrefix}{key}";
            if (!slots.Add(keyed))
            {
                throw new InvalidOperationException(
                    $"Two children of the same container have the key '{key}'. Give each child a unique key.");
            }

            return keyed;
        }

        if (IdentityOf(view) is { } identity)
        {
            string identified = $"{parentId}/{IdentitySlotPrefix}{identity}";
            if (slots.Add(identified))
            {
                return identified;
            }
        }

        Type type = view.GetType();
        ranks.TryGetValue(type, out int rank);
        ranks[type] = rank + 1;
        string ranked = $"{parentId}/{RankSlotPrefix}{type.Name}{RankSeparator}{rank.ToString(CultureInfo.InvariantCulture)}";
        slots.Add(ranked);
        return ranked;
    }

    // A component by its instance, a bound control by the property of its binding.
    private static string? IdentityOf(View view) => view switch
    {
        ScopeView { Identity: { } identity } => ReferenceKeys.Of(identity),
        { BindingTarget: { } target } => $"{ReferenceKeys.Of(target.Owner)}{PropertySeparator}{target.Property}",
        _ => null,
    };

    // Size and flexibility: an explicit size wins (.Width(72), or infinity to fill); otherwise a container is as flexible
    // as its children. The parent container reads these values to share its space.
    private static void Place(Node node, View view)
    {
        (bool fillWidth, bool fillHeight) = node.Adapter.FlexOf(node, view);
        node.FillWidth = view.Width is { } width ? float.IsPositiveInfinity(width) : fillWidth;
        node.FillHeight = view.Height is { } height ? float.IsPositiveInfinity(height) : fillHeight;
        node.Grow = view.Grow > 0 ? view.Grow : node.Adapter.GrowOf(node);

        Control root = node.Root;
        root.Width = view.Width is { } fixedWidth && !float.IsInfinity(fixedWidth) ? fixedWidth : double.NaN;
        root.Height = view.Height is { } fixedHeight && !float.IsInfinity(fixedHeight) ? fixedHeight : double.NaN;
        if (node.Decoration is not null)
        {
            node.Content.Width = double.NaN;
            node.Content.Height = double.NaN;
        }

        Flex.SetGrow(root, node.Grow);
        Flex.SetFillWidth(root, node.FillWidth);
        Flex.SetFillHeight(root, node.FillHeight);
        Flex.SetSelfAlign(root, view.SelfAlign);
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
            var slots = new HashSet<string>();
            var ranks = new Dictionary<Type, int>();
            int position = 0;
            foreach (View child in Ui.Unfold(group))
            {
                View placed = modified ? child.Inherit(modifiers, position) : child;
                position++;
                string childSlot = SlotOf(slot + ScopeContentSuffix, placed, slots, ranks);
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

            scope = new Scope(host.RequestUpdate, id, view.Identity);
            _scopes[id] = scope;
            if (view.Identity is Component component)
            {
                // Gives the component access to the theme catalog (Component.Themes).
                component.Host = host;
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
        if (scope.Identity is Component component && ReferenceEquals(component.Host, host))
        {
            component.Host = null;
        }
    }

    // Wraps a view whose control does not draw its own background, border, corners and padding (text, stack,
    // checkbox…), or a clickable view (.OnTap), in a Surface, only when the view needs one. The control moves into the
    // surface, and out of it, without being recreated.
    private void Decorate(Node node, View view, Theme theme)
    {
        if (node.Content is Surface)
        {
            // A box is already a surface: its adapter dresses it.
            return;
        }

        bool decorated = view.Background is not null
            || view.BorderColor is not null
            || view.CornerRadius is not null
            || view.Padding is not null;
        bool needed = view.TapAction is not null || (decorated && !node.Adapter.DrawsDecoration);
        if (needed && node.Decoration is null)
        {
            Detach(node.Content);
            node.Decoration = new Surface { Child = node.Content, TapAction = () => Run(node.View.TapAction) };
        }
        else if (!needed && node.Decoration is { } unused)
        {
            unused.Child = null;
            node.Decoration = null;
        }

        if (node.Decoration is not { } surface)
        {
            return;
        }

        if (node.Adapter.DrawsDecoration)
        {
            // The control keeps its own background; the surface only adds the click.
            surface.Paint(null, theme, tappable: true);
        }
        else
        {
            ApplyDecoration(surface, view, theme);
        }
    }

    // Visited before the children of the node, so that the order of the visits is the order of the tree.
    private void TrackLifecycle(Node node, View view)
    {
        if (view.AppearAction is null && view.DisappearAction is null && view.TaskAction is null)
        {
            return;
        }

        if (node.Lifecycle is not { } lifecycle)
        {
            lifecycle = new Lifecycle(node);
            node.Lifecycle = lifecycle;
            _lifecycles.Add(lifecycle);
        }

        lifecycle.Owner = _owners.TryPeek(out Scope? owner) ? owner : null;
        lifecycle.LastVisit = _pass;
        lifecycle.Depth = _depth;
        lifecycle.Order = ++_visits;
    }

    // A lifecycle is alive when its node was visited, or when the scope that declares it was kept by the update without
    // being re-evaluated: the nodes of such a scope are not visited, and without this rule a child component would
    // disappear whenever its parent is re-evaluated. The actions run after the update, on the dispatcher, so that a
    // state they change schedules the next update instead of changing a Body being computed: the disappearances first,
    // children before their parents, then the appearances and the restarted tasks, parents before their children. A
    // task is cancelled at once, when the update decides it.
    private void SweepLifecycles()
    {
        List<Lifecycle>? gone = null;
        List<Lifecycle>? appearing = null;
        List<Lifecycle>? restarting = null;
        foreach (Lifecycle lifecycle in _lifecycles)
        {
            if (lifecycle.LastVisit != _pass && !IsKept(lifecycle.Owner))
            {
                (gone ??= []).Add(lifecycle);
            }
            else if (!lifecycle.Appeared)
            {
                lifecycle.Appeared = true;
                DecideTask(lifecycle);
                (appearing ??= []).Add(lifecycle);
            }
            else if (TaskChanged(lifecycle))
            {
                lifecycle.Cancellation?.Cancel();
                DecideTask(lifecycle);
                if (lifecycle.TaskStarted)
                    (restarting ??= []).Add(lifecycle);
            }
        }

        if (gone is null && appearing is null && restarting is null)
        {
            return;
        }

        gone?.ForEach(Forget);
        gone?.Sort(Lifecycle.ChildrenFirst);
        appearing?.Sort(Lifecycle.ParentsFirst);
        restarting?.Sort(Lifecycle.ParentsFirst);
        Dispatcher.UIThread.Post(() =>
        {
            gone?.ForEach(Disappear);
            appearing?.ForEach(Appear);
            restarting?.ForEach(StartTask);
        });
    }

    // The task was added to a view that had appeared, removed from it, or its key changed.
    private static bool TaskChanged(Lifecycle lifecycle)
    {
        View view = lifecycle.Node.View;
        return view.TaskAction is null
            ? lifecycle.TaskStarted
            : !lifecycle.TaskStarted || !Equals(lifecycle.TaskKey, view.TaskKey);
    }

    private static void DecideTask(Lifecycle lifecycle)
    {
        lifecycle.TaskStarted = lifecycle.Node.View.TaskAction is not null;
        lifecycle.TaskKey = lifecycle.Node.View.TaskKey;
    }

    // Kept by the update: visited, not re-evaluated, and still the scope at its place.
    private bool IsKept(Scope? owner) =>
        owner is not null
        && owner.LastVisit == _pass
        && owner.EvaluatedPass != _pass
        && _scopes.TryGetValue(owner.Id, out Scope? live)
        && ReferenceEquals(live, owner);

    private void Forget(Lifecycle lifecycle)
    {
        _lifecycles.Remove(lifecycle);
        lifecycle.Gone = true;
        lifecycle.Cancellation?.Cancel();
        if (ReferenceEquals(lifecycle.Node.Lifecycle, lifecycle))
        {
            lifecycle.Node.Lifecycle = null;
        }
    }

    // A view that went away before its posted appearance ran gets neither OnAppear nor OnDisappear, nor its task.
    private void Appear(Lifecycle lifecycle)
    {
        if (lifecycle.Gone)
        {
            return;
        }

        lifecycle.AppearRan = true;
        run(lifecycle.Node.View.AppearAction);
        StartTask(lifecycle);
    }

    // Started on the user interface thread, after the update: after an await, the task resumes there. A run that is
    // still going (two restarts decided before the first one started) is cancelled first, so that one run at most goes on.
    private void StartTask(Lifecycle lifecycle)
    {
        if (lifecycle.Gone || !lifecycle.TaskStarted || lifecycle.Node.View.TaskAction is not { } task)
        {
            return;
        }

        lifecycle.Cancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        lifecycle.Cancellation = cancellation;
        run(() => _ = WatchAsync(lifecycle, task, cancellation));
    }

    // Runs the task until it ends. An OperationCanceledException after cancellation is expected; another exception is
    // reported as the exception of an action is. The cancellation is disposed when the task ends.
    private async Task WatchAsync(Lifecycle lifecycle, Func<CancellationToken, Task> task, CancellationTokenSource cancellation)
    {
        try
        {
            await task(cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Expected: the view disappeared, the key changed, or the window closed.
        }
        catch (Exception exception)
        {
            host.Report(exception);
        }
        finally
        {
            if (ReferenceEquals(lifecycle.Cancellation, cancellation))
            {
                lifecycle.Cancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private void Disappear(Lifecycle lifecycle)
    {
        if (lifecycle.AppearRan)
        {
            run(lifecycle.Node.View.DisappearAction);
        }
    }

    // The window closes: the dispatcher may not run again, so the views that appeared disappear at once, and their tasks
    // are cancelled.
    private void ReleaseLifecycles()
    {
        List<Lifecycle> all = [.. _lifecycles];
        all.Sort(Lifecycle.ChildrenFirst);
        all.ForEach(Forget);
        all.ForEach(Disappear);
    }
}
