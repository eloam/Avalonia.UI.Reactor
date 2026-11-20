using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Résout l'apparence sur mesure d'un contrôle : celle posée sur la vue, sinon celle du thème.
/// null = le contrôle garde son dessin standard. L'état passé au modèle
/// (survol, appui, focus) est celui du contrôle Avalonia.
/// </summary>
internal static class ControlTemplates
{
    public static View? Build(View view, Theme theme, bool hovered, bool pressed, bool focused) => view switch
    {
        ButtonView b when (b.CustomTemplate ?? theme.Button.Template) is { } t =>
            t(new ButtonState(b.Label, VariantOf(b, theme), hovered, pressed)),
        CheckboxView c when (c.CustomTemplate ?? theme.Checkbox.Template) is { } t =>
            t(new CheckboxState(c.IsChecked, c.Label, hovered, pressed)),
        TextFieldView f when (f.CustomTemplate ?? theme.TextField.Template) is { } t =>
            t(new TextFieldState(f.Value, f.Placeholder, focused, hovered)),
        _ => null,
    };

    /// <summary>Style d'un bouton : variante posée sur la vue, sinon style nommé, sinon style standard.</summary>
    public static ButtonVariant VariantOf(ButtonView b, Theme theme) =>
        b.Variant ?? (b.StyleName is { } name ? theme.Button[name] : theme.Button[b.Style]);
}
