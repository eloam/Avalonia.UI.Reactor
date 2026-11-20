using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// <c>Checkbox</c> : une <see cref="ReactorCheckBox"/>, case dessinée par le thème, libellé à côté ; avec un modèle, la vue
/// du modèle (case et libellé), recalculée quand l'état change. La case est contrôlée : un clic propose la nouvelle
/// valeur à l'état (<c>OnChange</c>), puis la case affiche celle de la vue.
/// </summary>
internal sealed class CheckboxAdapter : ViewAdapter<CheckboxView, ReactorCheckBox>
{
    public static readonly CheckboxAdapter Instance = new();

    protected override void Attach(Reconciler reconciler, Node node, ReactorCheckBox box)
    {
        box.IsCheckedChanged += (_, _) =>
        {
            if (node.Applying) return;
            bool value = box.IsChecked == true;
            reconciler.Run(() => ((CheckboxView)node.View).OnChange?.Invoke(value));
            Reconciler.Apply(node, () => box.IsChecked = ((CheckboxView)node.View).IsChecked);
        };
        box.PropertyChanged += (_, e) =>
        {
            if (e.Property == InputElement.IsPointerOverProperty || e.Property == ToggleButton.IsPressedProperty)
                Paint(reconciler, node, box);
        };
    }

    protected override void Update(Reconciler reconciler, Node node, ReactorCheckBox box, CheckboxView view, ViewEnvironment env)
    {
        box.IsChecked = view.IsChecked;
        box.SetFont(env.FontFor(view));
        box.SetBrush(TemplatedControl.ForegroundProperty, env.ForegroundFor(view));
        Paint(reconciler, node, box);
    }

    static void Paint(Reconciler reconciler, Node node, ReactorCheckBox box)
    {
        var view = (CheckboxView)node.View;
        var theme = node.Environment.Theme;
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
