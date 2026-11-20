using Avalonia.Controls.Primitives;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// <c>Checkbox</c> : une <see cref="ReactorCheckBox"/>, case dessinée par le thème, libellé à côté. La case est contrôlée :
/// un clic propose la nouvelle valeur à l'état (<c>OnChange</c>), puis la case affiche celle de la vue.
/// </summary>
internal sealed class CheckboxAdapter : ViewAdapter<CheckboxView, ReactorCheckBox>
{
    public static readonly CheckboxAdapter Instance = new();

    protected override void Attach(Reconciler reconciler, Node node, ReactorCheckBox box) =>
        box.IsCheckedChanged += (_, _) =>
        {
            if (node.Applying) return;
            bool value = box.IsChecked == true;
            reconciler.Run(() => ((CheckboxView)node.View).OnChange?.Invoke(value));
            Reconciler.Apply(node, () => box.IsChecked = ((CheckboxView)node.View).IsChecked);
        };

    protected override void Update(Reconciler reconciler, Node node, ReactorCheckBox box, CheckboxView view, ViewEnvironment env)
    {
        box.IsChecked = view.IsChecked;
        box.Content = view.Label;
        box.Look = env.Theme;
        box.SetFont(env.FontFor(view));
        box.SetBrush(TemplatedControl.ForegroundProperty, env.ForegroundFor(view));
    }
}
