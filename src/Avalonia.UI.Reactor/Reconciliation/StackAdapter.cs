// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Displays <c>VStack</c> and <c>HStack</c> with a <see cref="FlexStack"/>, spaced by the theme unless
/// <c>.Spacing(…)</c> is set; the shared wrapper draws the background and the border.
/// </summary>
internal sealed class StackAdapter : ViewAdapter<StackView, FlexStack>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly StackAdapter Instance = new();

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, FlexStack stack, StackView view, ViewEnvironment environment)
    {
        Theme theme = environment.Theme;
        stack.Axis = view.Axis;
        stack.Spacing = view.Spacing ?? theme.Spacing;
        stack.CrossAlign = view.CrossAlign;

        // The children inherit the text style of the stack (.Font and .Foreground set on it).
        ViewEnvironment children = environment with
        {
            Font = environment.FontFor(view),
            Foreground = environment.ForegroundFor(view),
        };
        Reconciler.Arrange(stack.Children, reconciler.ReconcileChildren(node, view.Children, children));
    }

    /// <summary>
    /// Returns the flexibility of a stack: it stretches on its axis when a child stretches or has a share of space, and on
    /// the other axis when a child fills it.
    /// </summary>
    /// <param name="node">The node of the stack.</param>
    /// <param name="view">The view of the stack.</param>
    /// <returns>Whether the stack fills the available width and height.</returns>
    protected override (bool Width, bool Height) FlexOf(Node node, StackView view) => view.Axis == Primitives.Axis.Horizontal
        ? (node.Children.Any(c => c.Grow > 0 || c.FillWidth), node.Children.Any(c => c.FillHeight))
        : (node.Children.Any(c => c.FillWidth), node.Children.Any(c => c.Grow > 0 || c.FillHeight));
}
