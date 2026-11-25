// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls.Primitives;
using Avalonia.Input;
using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;

namespace MartletUI.Reconciliation;

/// <summary>
/// Displays a <c>Checkbox</c> view with a <see cref="MartletCheckBox"/>: a box drawn by the theme with the label next to
/// it, or the view of the template (box and label), recomputed when the state changes.
/// </summary>
/// <remarks>
/// The checkbox is controlled: a click proposes the new value to the state (<c>OnChange</c>), then the box shows the
/// value of the view.
/// </remarks>
internal sealed class CheckboxAdapter : ViewAdapter<CheckboxView, MartletCheckBox>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly CheckboxAdapter Instance = new();

    /// <inheritdoc/>
    protected override void Attach(Reconciler reconciler, Node node, MartletCheckBox box)
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
    protected override void Update(Reconciler reconciler, Node node, MartletCheckBox box, CheckboxView view, ViewEnvironment environment)
    {
        box.IsChecked = view.IsChecked;
        box.SetFont(environment.FontFor(view));
        box.SetBrush(TemplatedControl.ForegroundProperty, environment.ForegroundFor(view));
        Paint(reconciler, node, box);
    }

    private static void Paint(Reconciler reconciler, Node node, MartletCheckBox box)
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
