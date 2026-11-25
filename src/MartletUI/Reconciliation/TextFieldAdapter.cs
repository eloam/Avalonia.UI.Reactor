// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;

namespace MartletUI.Reconciliation;

/// <summary>
/// Displays a <c>TextField</c> view with a <see cref="MartletTextBox"/> (selection, clipboard, undo, input methods) in
/// the colors of the theme: the hover border under the pointer, the focus border while typing.
/// </summary>
/// <remarks>
/// The field is controlled: each input goes to the state (<c>OnChange</c>), then the field shows the value of the view,
/// so that state that rejects or transforms the input has the last word. Enter runs <c>OnSubmit</c>.
/// </remarks>
internal sealed class TextFieldAdapter : ViewAdapter<TextFieldView, MartletTextBox>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly TextFieldAdapter Instance = new();

    /// <inheritdoc/>
    public override bool DrawsDecoration => true;

    /// <inheritdoc/>
    protected override void Attach(Reconciler reconciler, Node node, MartletTextBox field)
    {
        // The Text property, not the TextChanged event: Avalonia raises the event later on the dispatcher, and an action
        // that immediately follows the input (Enter, a click, a shortcut) would still read the previous state.
        field.PropertyChanged += (_, e) =>
        {
            if (e.Property == InputElement.IsPointerOverProperty || e.Property == InputElement.IsFocusedProperty)
            {
                Paint(reconciler, node, field);
                return;
            }

            if (e.Property != TextBox.TextProperty || node.Applying)
            {
                return;
            }

            string text = field.Text ?? string.Empty;
            reconciler.Run(() => ((TextFieldView)node.View).OnChange?.Invoke(text));
            Reconciler.Apply(node, () =>
            {
                string shown = ((TextFieldView)node.View).Value;
                if (field.Text != shown)
                {
                    field.Text = shown;
                }
            });
        };

        // Before the field (tunnel): a single-line field has no use for Enter, the view does.
        field.AddHandler(
            InputElement.KeyDownEvent,
            (_, e) =>
            {
                if (e.Key != Key.Enter || ((TextFieldView)node.View).OnSubmit is not { } submit)
                {
                    return;
                }

                e.Handled = true;
                reconciler.Run(submit);
            },
            RoutingStrategies.Tunnel);
    }

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, MartletTextBox field, TextFieldView view, ViewEnvironment environment)
    {
        Theme theme = environment.Theme;
        TextFieldTheme style = theme.TextField;

        if (field.Text != view.Value)
        {
            field.Text = view.Value;
        }

        field.PlaceholderText = view.Placeholder;
        field.SetEditMenu(style.EditMenu);
        field.SetFont(environment.FontFor(view));
        field.SetBrush(TemplatedControl.ForegroundProperty, theme[view.Foreground ?? style.Foreground]);
        field.SetBrush(TextBox.PlaceholderForegroundProperty, theme[style.Placeholder]);
        field.SetBrush(TextBox.CaretBrushProperty, theme[style.Caret]);
        field.SetBrush(TextBox.SelectionBrushProperty, theme[style.Selection].WithAlpha(style.SelectionOpacity));
        field.CaretBlinkInterval = TimeSpan.FromMilliseconds(style.CaretBlinkMs);
        field.MinHeight = 0;
        field.VerticalContentAlignment = VerticalAlignment.Center;
        Paint(reconciler, node, field);
    }

    // The look for the current state (hover, focus). With a template, the view of the template draws the field and the
    // editing area is placed on its field.Editor. Without one: the frame of the theme, with the focus border while
    // typing, the hover border under the pointer, otherwise the border of the view; a thicker border takes its space from
    // the padding, so the text does not move when the field gets the focus.
    private static void Paint(Reconciler reconciler, Node node, MartletTextBox field)
    {
        var view = (TextFieldView)node.View;
        Theme theme = node.Environment.Theme;
        TextFieldTheme style = theme.TextField;
        bool focused = field.IsFocused;
        bool hovered = field.IsPointerOver;

        if (ControlTemplates.Build(view, theme, hovered, pressed: false, focused) is { } template)
        {
            field.Theme = MartletThemes.TemplatedTextBox;
            field.CustomContent = reconciler.RenderTemplate(node, template);
            field.ClearValue(TemplatedControl.BackgroundProperty);
            field.ClearValue(TemplatedControl.BorderBrushProperty);
            field.BorderThickness = default;
            field.CornerRadius = default;

            // The template handles its own spacing.
            field.Padding = default;
            field.MinWidth = 0;
            return;
        }

        node.Template = null;
        field.Theme = MartletThemes.TextBox;
        field.CustomContent = null;
        float restWidth = view.BorderWidth ?? style.BorderWidth;
        ColorValue color = focused ? style.FocusBorderColor
            : hovered ? style.HoverBorderColor
            : view.BorderColor ?? style.BorderColor;
        float width = focused ? style.FocusBorderWidth : restWidth;
        Primitives.Thickness padding = view.Padding ?? style.Padding;
        float shift = restWidth - width;
        field.SetBrush(TemplatedControl.BackgroundProperty, theme[view.Background ?? style.Background]);
        field.SetBrush(TemplatedControl.BorderBrushProperty, theme[color]);
        field.BorderThickness = new Thickness(width);
        field.CornerRadius = new CornerRadius(view.CornerRadius ?? style.Radius);
        field.Padding = new Thickness(
            Math.Max(0, padding.Left + shift),
            Math.Max(0, padding.Top + shift),
            Math.Max(0, padding.Right + shift),
            Math.Max(0, padding.Bottom + shift));

        // The natural width comes from the theme; the typed text does not change it.
        field.MinWidth = style.MinWidth + padding.Horizontal;
    }
}
