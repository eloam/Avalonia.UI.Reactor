// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Displays a <c>Divider()</c> with a solid line, with the thickness and color of the theme
/// (<see cref="Theme.DividerThickness"/>, <see cref="Theme.DividerColor"/>); <c>.Height(…)</c> and
/// <c>.Background(…)</c> replace them.
/// </summary>
internal sealed class DividerAdapter : ViewAdapter<DividerView, Border>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly DividerAdapter Instance = new();

    /// <summary>
    /// Gets a value indicating that the line draws its own background, which is its color: no wrapper is needed.
    /// </summary>
    public override bool DrawsDecoration => true;

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, Border line, DividerView view, ViewEnvironment environment)
    {
        Theme theme = environment.Theme;
        line.SetBrush(Border.BackgroundProperty, theme[view.Background ?? theme.DividerColor]);

        // The natural height is the thickness of the theme, unless the view sets a height (applied with its size).
        line.MinHeight = view.Height is null ? theme.DividerThickness : 0;
    }
}
