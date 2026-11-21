// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Displays a <c>Checkbox</c> view with a <see cref="ReactorCheckBox"/>: a box drawn by the theme with the label next to
/// it, or the view of the template (box and label), recomputed when the state changes.
/// </summary>
/// <remarks>
/// The checkbox is controlled: a click proposes the new value to the state (<c>OnChange</c>), then the box shows the
/// value of the view.
/// </remarks>
internal sealed class CheckboxAdapter : ViewAdapter<CheckboxView, ReactorCheckBox>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly CheckboxAdapter Instance = new();

    /// <inheritdoc/>
    protected override void Attach(Reconciler reconciler, Node node, ReactorCheckBox box)
    {
        box.IsCheckedChanged += (_, _) =>
        {
            if (node.Applying)
            {
                return;
            }

            bool value = box.IsChecked == true;
            reconciler.Run(() => ((CheckboxView)node.View).OnChange?.Invoke(value));
            Reconciler.Apply(node, () => box.IsChecked = ((CheckboxView)node.View).IsChecked);
        };
        box.PropertyChanged += (_, e) =>
        {
            if (e.Property == InputElement.IsPointerOverProperty || e.Property == ToggleButton.IsPressedProperty)
            {
                Paint(reconciler, node, box);
            }
        };
    }

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, ReactorCheckBox box, CheckboxView view, ViewEnvironment environment)
    {
        box.IsChecked = view.IsChecked;
        box.SetFont(environment.FontFor(view));
        box.SetBrush(TemplatedControl.ForegroundProperty, environment.ForegroundFor(view));
        Paint(reconciler, node, box);
    }

    private static void Paint(Reconciler reconciler, Node node, ReactorCheckBox box)
    {
        var view = (CheckboxView)node.View;
        Theme theme = node.Environment.Theme;
        bool hovered = box.IsPointerOver;
        if (ControlTemplates.Build(view, theme, hovered, pressed: hovered && box.IsPressed, focused: false) is { } template)
        {
            box.Content = reconciler.RenderTemplate(node, template);
            box.ShowsGlyph = false;
        }
        else
        {
            node.Template = null;
            box.Content = view.Label;
            box.ShowsGlyph = true;
        }

        box.Look = theme;
    }
}
