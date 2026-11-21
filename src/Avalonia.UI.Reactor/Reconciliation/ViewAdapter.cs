// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents the kind of control that displays a type of view: it creates the control once, then applies each new
/// version of the view to it.
/// </summary>
/// <remarks>
/// An adapter has no state: it works on the node it receives.
/// </remarks>
internal abstract class ViewAdapter
{
    /// <summary>
    /// Gets a value indicating whether the control draws the background, border, corners and padding of the view itself;
    /// otherwise a wrapper does.
    /// </summary>
    public virtual bool DrawsDecoration => false;

    /// <summary>
    /// Returns the adapter of a view.
    /// </summary>
    /// <param name="view">The view.</param>
    /// <returns>The adapter of the type of the view.</returns>
    /// <exception cref="NotSupportedException">The type of the view has no adapter.</exception>
    public static ViewAdapter For(View view) => view switch
    {
        TextView => TextAdapter.Instance,
        ButtonView => ButtonAdapter.Instance,
        StackView => StackAdapter.Instance,
        SpacerView => SpacerAdapter.Instance,
        TextFieldView => TextFieldAdapter.Instance,
        CheckboxView => CheckboxAdapter.Instance,
        DividerView => DividerAdapter.Instance,
        BoxView => BoxAdapter.Instance,
        ZStackView => ZStackAdapter.Instance,
        CanvasView => CanvasAdapter.Instance,
        MenuBarView => MenuBarAdapter.Instance,
        EditorView => EditorAdapter.Instance,
        NativeView => NativeAdapter.Instance,
        _ => throw new NotSupportedException(
            $"The view '{view.GetType().Name}' is not supported. Wrap an Avalonia control with 'Native' to display it."),
    };

    /// <summary>
    /// Creates the control of a node, once per node.
    /// </summary>
    /// <param name="reconciler">The reconciler.</param>
    /// <param name="node">The node.</param>
    /// <returns>The new control.</returns>
    public abstract Control Create(Reconciler reconciler, Node node);

    /// <summary>
    /// Applies a view to the control of a node: properties, then children.
    /// </summary>
    /// <param name="reconciler">The reconciler.</param>
    /// <param name="node">The node.</param>
    /// <param name="view">The view to apply.</param>
    /// <param name="environment">The environment of the view.</param>
    public abstract void Update(Reconciler reconciler, Node node, View view, ViewEnvironment environment);

    /// <summary>
    /// Returns the flexibility that the control gets from its children: a stack that contains a <c>Spacer()</c>
    /// stretches, as in SwiftUI.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="view">The view.</param>
    /// <returns>Whether the control fills the available width and height.</returns>
    public virtual (bool Width, bool Height) FlexOf(Node node, View view) => (false, false);

    /// <summary>
    /// Returns the share of space that the control gets from its children when the view declares none (for a ZStack,
    /// that of its content).
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The share of the remaining space.</returns>
    public virtual float GrowOf(Node node) => 0;

    /// <summary>
    /// Determines whether the node that held the place can display a view (same kind of control).
    /// </summary>
    /// <param name="existing">The node that held the place.</param>
    /// <param name="view">The new view.</param>
    /// <returns><see langword="true"/> if the node can be reused; otherwise, <see langword="false"/>.</returns>
    public virtual bool CanReuse(Node existing, View view) => true;
}

/// <summary>
/// Represents an adapter that receives the view and the control with their actual types.
/// </summary>
/// <typeparam name="TView">The type of view.</typeparam>
/// <typeparam name="TControl">The type of control.</typeparam>
internal abstract class ViewAdapter<TView, TControl> : ViewAdapter
    where TView : View
    where TControl : Control, new()
{
    /// <inheritdoc/>
    public sealed override Control Create(Reconciler reconciler, Node node)
    {
        var control = new TControl();
        Attach(reconciler, node, control);
        return control;
    }

    /// <inheritdoc/>
    public sealed override void Update(Reconciler reconciler, Node node, View view, ViewEnvironment environment) =>
        Update(reconciler, node, (TControl)node.Content, (TView)view, environment);

    /// <inheritdoc/>
    public sealed override (bool Width, bool Height) FlexOf(Node node, View view) => FlexOf(node, (TView)view);

    /// <summary>
    /// Connects the events of the control, once; they read the current view of the node.
    /// </summary>
    /// <param name="reconciler">The reconciler.</param>
    /// <param name="node">The node.</param>
    /// <param name="control">The new control.</param>
    protected virtual void Attach(Reconciler reconciler, Node node, TControl control)
    {
    }

    /// <summary>
    /// Applies a view to the control of a node.
    /// </summary>
    /// <param name="reconciler">The reconciler.</param>
    /// <param name="node">The node.</param>
    /// <param name="control">The control of the node.</param>
    /// <param name="view">The view to apply.</param>
    /// <param name="environment">The environment of the view.</param>
    protected abstract void Update(Reconciler reconciler, Node node, TControl control, TView view, ViewEnvironment environment);

    /// <summary>
    /// Returns the flexibility that the control gets from its children.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="view">The view.</param>
    /// <returns>Whether the control fills the available width and height.</returns>
    protected virtual (bool Width, bool Height) FlexOf(Node node, TView view) => (false, false);
}
