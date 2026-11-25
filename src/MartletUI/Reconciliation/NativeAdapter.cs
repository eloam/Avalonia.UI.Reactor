// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;

namespace MartletUI.Reconciliation;

/// <summary>
/// Displays <c>Native(…)</c>: the control of the application, in a <see cref="NativeHost"/> that tracks its update.
/// </summary>
internal sealed class NativeAdapter : ViewAdapter
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly NativeAdapter Instance = new();

    /// <inheritdoc/>
    public override Control Create(Reconciler reconciler, Node node) =>
        new NativeHost { Child = ((NativeView)node.View).Create() };

    /// <inheritdoc/>
    public override void Update(Reconciler reconciler, Node node, View view, ViewEnvironment environment)
    {
        var native = (NativeView)view;
        var host = (NativeHost)node.Content;
        if (native.Update is { } update && host.Child is { } control)
        {
            host.Apply(() => update(control));
        }
    }

    /// <summary>
    /// Determines whether the node that held the place can display the view: the control is reused as long as the view
    /// asks for a control of the same type.
    /// </summary>
    /// <param name="existing">The node that held the place.</param>
    /// <param name="view">The new view.</param>
    /// <returns><see langword="true"/> if the control has the requested type; otherwise, <see langword="false"/>.</returns>
    public override bool CanReuse(Node existing, View view) =>
        existing.View is NativeView previous && previous.ControlType == ((NativeView)view).ControlType;
}
