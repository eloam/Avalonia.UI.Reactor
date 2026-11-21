// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Displays a <c>Button</c> view with a <see cref="ReactorButton"/>, in the colors of its style (<c>.Style(…)</c>) in
/// the theme, with the overlay of the style under the pointer and while pressed.
/// </summary>
/// <remarks>
/// With a template (<c>.Template(…)</c> or the theme's), the button shows the view of the template, recomputed when its
/// state changes. A click runs the action of the current view.
/// </remarks>
internal sealed class ButtonAdapter : ViewAdapter<ButtonView, ReactorButton>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly ButtonAdapter Instance = new();

    /// <inheritdoc/>
    public override bool DrawsDecoration => true;

    /// <inheritdoc/>
    protected override void Attach(Reconciler reconciler, Node node, ReactorButton button)
    {
        button.Click += (_, _) => reconciler.Run(((ButtonView)node.View).OnClick);
        button.PropertyChanged += (_, e) =>
        {
            if (e.Property == InputElement.IsPointerOverProperty || e.Property == Button.IsPressedProperty)
            {
                Paint(reconciler, node, button);
            }
        };
    }

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, ReactorButton button, ButtonView view, ViewEnvironment environment)
    {
        button.SetFont(environment.FontFor(view));
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        Paint(reconciler, node, button);
    }

    // The look for the current state (hover, pressed): the view of the template if there is one, otherwise the colors of
    // the style, with an overlay under the pointer and a stronger one while pressed.
    private static void Paint(Reconciler reconciler, Node node, ReactorButton button)
    {
        var view = (ButtonView)node.View;
        Theme theme = node.Environment.Theme;
        ButtonVariant variant = ControlTemplates.VariantOf(view, theme);
        bool hovered = button.IsPointerOver;
        bool pressed = hovered && button.IsPressed;

        if (ControlTemplates.Build(view, theme, hovered, pressed, focused: false) is { } template)
        {
            button.Content = reconciler.RenderTemplate(node, template);
            button.ClearValue(TemplatedControl.BackgroundProperty);
            button.ClearValue(TemplatedControl.BorderBrushProperty);
            button.BorderThickness = default;
            button.CornerRadius = default;

            // The template handles its own spacing.
            button.Padding = default;
            return;
        }

        node.Template = null;
        ColorValue? border = view.BorderColor ?? variant.BorderColor;
        float overlay = pressed ? variant.PressedOverlay : hovered ? variant.HoverOverlay : 0;
        button.Content = view.Label;
        button.SetBrush(TemplatedControl.BackgroundProperty, theme.Overlay(theme[view.Background ?? variant.Background], overlay));
        button.SetBrush(TemplatedControl.ForegroundProperty, theme[view.Foreground ?? variant.Foreground]);
        button.SetBrush(TemplatedControl.BorderBrushProperty, border?.Resolve(theme.Palette));
        button.BorderThickness = new Thickness(border is null ? 0 : view.BorderWidth ?? theme.Button.BorderWidth);
        button.CornerRadius = new CornerRadius(view.CornerRadius ?? theme.Button.Radius);
        button.Padding = (view.Padding ?? theme.Button.Padding).ToAvalonia();
    }
}
