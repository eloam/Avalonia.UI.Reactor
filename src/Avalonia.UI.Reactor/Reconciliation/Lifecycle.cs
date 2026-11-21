// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.State;

/// <summary>
/// Represents the lifecycle of a node whose view declares <c>OnAppear</c> or <c>OnDisappear</c>.
/// </summary>
/// <remarks>
/// The lifecycle follows the node, not its control: a control moved into a decoration is detached from the visual tree
/// and attached again, which changes nothing here.
/// </remarks>
/// <param name="node">The node.</param>
internal sealed class Lifecycle(Node node)
{
    /// <summary>
    /// Gets the order in which lifecycles appear: by depth, parents before their children, then in tree order.
    /// </summary>
    public static Comparison<Lifecycle> ParentsFirst { get; } =
        (a, b) => a.Depth != b.Depth ? a.Depth.CompareTo(b.Depth) : a.Order.CompareTo(b.Order);

    /// <summary>
    /// Gets the order in which lifecycles disappear: children before their parents.
    /// </summary>
    public static Comparison<Lifecycle> ChildrenFirst { get; } = (a, b) => ParentsFirst(b, a);

    /// <summary>
    /// Gets the node.
    /// </summary>
    public Node Node { get; } = node;

    /// <summary>
    /// Gets or sets the innermost scope that declared the view at its last visit, or <see langword="null"/>.
    /// </summary>
    public Scope? Owner { get; set; }

    /// <summary>
    /// Gets or sets the number of the last update that visited the node.
    /// </summary>
    public int LastVisit { get; set; }

    /// <summary>
    /// Gets or sets the depth of the node in the tree at its last visit.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Gets or sets the order of the last visit, in tree order.
    /// </summary>
    public long Order { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the appearance of the view was decided by an update.
    /// </summary>
    public bool Appeared { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the <c>OnAppear</c> action ran (it runs after the update that decided it).
    /// </summary>
    public bool AppearRan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the view left the tree for good.
    /// </summary>
    public bool Gone { get; set; }
}
