using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// <c>Button</c> : un <see cref="Button"/> d'Avalonia, aux couleurs de sa variante (<c>.Style(…)</c>) dans le thème.
/// Le clic exécute l'action de la vue courante, puis l'interface se met à jour.
/// </summary>
internal sealed class ButtonAdapter : ViewAdapter<ButtonView, Button>
{
    public static readonly ButtonAdapter Instance = new();

    public override bool DrawsDecoration => true;

    protected override void Attach(Reconciler reconciler, Node node, Button button) =>
        button.Click += (_, _) => reconciler.Run(((ButtonView)node.View).OnClick);

    protected override void Update(Reconciler reconciler, Node node, Button button, ButtonView view, ViewEnvironment env)
    {
        var theme = env.Theme;
        var variant = ControlTemplates.VariantOf(view, theme);
        var border = view.BorderColor ?? variant.BorderColor;

        button.Content = view.Label;
        button.SetFont(env.FontFor(view));
        button.SetBrush(TemplatedControl.BackgroundProperty, theme[view.Background ?? variant.Background]);
        button.SetBrush(TemplatedControl.ForegroundProperty, theme[view.Foreground ?? variant.Foreground]);
        button.SetBrush(TemplatedControl.BorderBrushProperty, border?.Resolve(theme.Palette));
        button.BorderThickness = new Thickness(border is null ? 0 : view.BorderWidth ?? theme.Button.BorderWidth);
        button.CornerRadius = new CornerRadius(view.CornerRadius ?? theme.Button.Radius);
        button.Padding = (view.Padding ?? theme.Button.Padding).ToAvalonia();
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
    }
}
