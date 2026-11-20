using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// <c>Button</c> : un <see cref="ReactorButton"/>, aux couleurs de sa variante (<c>.Style(…)</c>) dans le thème ; au
/// survol et à l'appui, le voile de la variante. Le clic exécute l'action de la vue courante, puis l'interface se met à jour.
/// </summary>
internal sealed class ButtonAdapter : ViewAdapter<ButtonView, ReactorButton>
{
    public static readonly ButtonAdapter Instance = new();

    public override bool DrawsDecoration => true;

    protected override void Attach(Reconciler reconciler, Node node, ReactorButton button)
    {
        button.Click += (_, _) => reconciler.Run(((ButtonView)node.View).OnClick);
        button.PropertyChanged += (_, e) =>
        {
            if (e.Property == InputElement.IsPointerOverProperty || e.Property == Button.IsPressedProperty) Paint(node, button);
        };
    }

    protected override void Update(Reconciler reconciler, Node node, ReactorButton button, ButtonView view, ViewEnvironment env)
    {
        var theme = env.Theme;
        var variant = ControlTemplates.VariantOf(view, theme);
        var border = view.BorderColor ?? variant.BorderColor;

        button.Content = view.Label;
        button.SetFont(env.FontFor(view));
        button.SetBrush(TemplatedControl.ForegroundProperty, theme[view.Foreground ?? variant.Foreground]);
        button.SetBrush(TemplatedControl.BorderBrushProperty, border?.Resolve(theme.Palette));
        button.BorderThickness = new Thickness(border is null ? 0 : view.BorderWidth ?? theme.Button.BorderWidth);
        button.CornerRadius = new CornerRadius(view.CornerRadius ?? theme.Button.Radius);
        button.Padding = (view.Padding ?? theme.Button.Padding).ToAvalonia();
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        Paint(node, button);
    }

    /// <summary>Fond selon l'état : celui de la variante, voilé au survol, davantage à l'appui.</summary>
    static void Paint(Node node, ReactorButton button)
    {
        var view = (ButtonView)node.View;
        var theme = node.Environment.Theme;
        var variant = ControlTemplates.VariantOf(view, theme);
        bool hovered = button.IsPointerOver, pressed = hovered && button.IsPressed;
        float overlay = pressed ? variant.PressedOverlay : hovered ? variant.HoverOverlay : 0;
        button.SetBrush(TemplatedControl.BackgroundProperty, theme.Overlay(theme[view.Background ?? variant.Background], overlay));
    }
}
