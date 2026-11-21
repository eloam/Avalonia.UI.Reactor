// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Displays the <c>field.Editor</c> of a text field template with an <see cref="EditorSlot"/>, at the natural width of
/// the text fields of the theme.
/// </summary>
internal sealed class EditorAdapter : ViewAdapter<EditorView, EditorSlot>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly EditorAdapter Instance = new();

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, EditorSlot slot, EditorView view, ViewEnvironment environment) =>
        slot.Natural = slot.Natural.WithWidth(environment.Theme.TextField.MinWidth);
}
