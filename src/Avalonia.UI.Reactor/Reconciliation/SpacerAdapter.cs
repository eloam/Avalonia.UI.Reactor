// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Displays a <c>Spacer()</c> with an empty control; its share of space (<c>Grow = 1</c>) comes from the view, as for
/// any view.
/// </summary>
internal sealed class SpacerAdapter : ViewAdapter<SpacerView, Control>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly SpacerAdapter Instance = new();

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, Control spacer, SpacerView view, ViewEnvironment environment)
    {
    }
}
