// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Represents a live node of the reconciler: the Avalonia control that displays a view.
/// </summary>
/// <remarks>
/// A node is reused from one update to the next as long as its view keeps the same path and the same kind of control;
/// only its properties change then.
/// </remarks>
internal sealed class Node
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Node"/> class and creates its control.
    /// </summary>
    /// <param name="id">The path of the view in the tree.</param>
    /// <param name="adapter">The adapter of the view type.</param>
    /// <param name="view">The view.</param>
    /// <param name="create">The function that creates the control of the node.</param>
    public Node(string id, ViewAdapter adapter, View view, Func<Node, Control> create)
    {
        Id = id;
        Slot = id;
        Adapter = adapter;
        View = view;
        Content = create(this);
    }

    /// <summary>
    /// Gets the path of the view in the tree: <c>r.</c>, <c>r./0</c>, <c>r./todo-3</c>…
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the place where the parent put this node; it differs from <see cref="Id"/> for the content of a scope
    /// (<c>…/0</c> and <c>…/0.</c>).
    /// </summary>
    public string Slot { get; set; }

    /// <summary>
    /// Gets the adapter of the view type.
    /// </summary>
    public ViewAdapter Adapter { get; }

    /// <summary>
    /// Gets or sets the last view applied; the events of the control (a click…) read its action.
    /// </summary>
    public View View { get; set; }

    /// <summary>
    /// Gets or sets the environment of the view: the theme of its branch, the inherited font and color.
    /// </summary>
    public ViewEnvironment Environment { get; set; }

    /// <summary>
    /// Gets the control that displays the view.
    /// </summary>
    public Control Content { get; }

    /// <summary>
    /// Gets or sets the wrapper that draws the background, border, corners and padding when the control cannot, and
    /// receives the click of an <c>.OnTap</c> view.
    /// </summary>
    public Surface? Decoration { get; set; }

    /// <summary>
    /// Gets the control placed in the parent: the wrapper if there is one, otherwise the control.
    /// </summary>
    public Control Root => Decoration ?? Content;

    /// <summary>
    /// Gets or sets a value indicating whether the reconciler is setting the properties of the control: the events it
    /// raises then (text, check state) come from the view, not from the user, and must not go back to the state.
    /// </summary>
    public bool Applying { get; set; }

    /// <summary>
    /// Gets or sets the nodes of the children, in order.
    /// </summary>
    public IReadOnlyList<Node> Children { get; set; } = [];

    /// <summary>
    /// Gets or sets the node of the view of the template of the control (<c>.Template(…)</c> or the theme's), if any.
    /// </summary>
    public Node? Template { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the control takes all the available width (<c>.FillWidth()</c>, or a
    /// stack with such a child).
    /// </summary>
    public bool FillWidth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the control takes all the available height.
    /// </summary>
    public bool FillHeight { get; set; }

    /// <summary>
    /// Gets or sets the share of the remaining space on the axis of the parent stack.
    /// </summary>
    public float Grow { get; set; }
}
