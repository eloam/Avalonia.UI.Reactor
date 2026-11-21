// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Resolves the custom look of a control: the template set on the view, otherwise the template of the theme.
/// </summary>
internal static class ControlTemplates
{
    /// <summary>
    /// Builds the view of the template of a control, from the state of its Avalonia control.
    /// </summary>
    /// <param name="view">The view of the control.</param>
    /// <param name="theme">The theme of the control.</param>
    /// <param name="hovered">A value indicating whether the pointer is over the control.</param>
    /// <param name="pressed">A value indicating whether the control is pressed.</param>
    /// <param name="focused">A value indicating whether the control has the keyboard focus.</param>
    /// <returns>The view of the template, or <see langword="null"/> if the control keeps its standard look.</returns>
    public static View? Build(View view, Theme theme, bool hovered, bool pressed, bool focused) => view switch
    {
        ButtonView button when (button.CustomTemplate ?? theme.Button.Template) is { } template =>
            template(new ButtonState(button.Label, VariantOf(button, theme), hovered, pressed)),
        CheckboxView checkbox when (checkbox.CustomTemplate ?? theme.Checkbox.Template) is { } template =>
            template(new CheckboxState(checkbox.IsChecked, checkbox.Label, hovered, pressed)),
        TextFieldView field when (field.CustomTemplate ?? theme.TextField.Template) is { } template =>
            template(new TextFieldState(field.Value, field.Placeholder, focused, hovered)),
        _ => null,
    };

    /// <summary>
    /// Returns the colors of a button: its custom colors, otherwise its named style, otherwise its built-in style.
    /// </summary>
    /// <param name="button">The view of the button.</param>
    /// <param name="theme">The theme of the button.</param>
    /// <returns>The colors of the button.</returns>
    public static ButtonVariant VariantOf(ButtonView button, Theme theme) =>
        button.Variant ?? (button.StyleName is { } name ? theme.Button[name] : theme.Button[button.Style]);
}
