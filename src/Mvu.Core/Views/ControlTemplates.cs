using Mvu.Theming;

namespace Mvu.Views;

/// <summary>
/// Résout l'apparence sur mesure d'un contrôle : celle posée sur la vue, sinon celle du thème.
/// null = le contrôle garde son dessin standard. Pour les menus, <paramref name="highlighted"/> vaut « menu ouvert
/// ou titre actif au clavier » (titre) ou « élément surligné » (élément), et <paramref name="showMnemonic"/>
/// demande de souligner les lettres d'accès.
/// </summary>
internal static class ControlTemplates
{
    public static View? Build(View view, Theme theme, bool hovered, bool pressed, bool focused,
        bool highlighted = false, bool showMnemonic = false) => view switch
    {
        ButtonView b when (b.CustomTemplate ?? theme.Button.Template) is { } t =>
            t(new ButtonState(b.Label, VariantOf(b, theme), hovered, pressed)),
        CheckboxView c when (c.CustomTemplate ?? theme.Checkbox.Template) is { } t =>
            t(new CheckboxState(c.IsChecked, c.Label, hovered, pressed)),
        TextFieldView f when (f.CustomTemplate ?? theme.TextField.Template) is { } t =>
            t(new TextFieldState(f.Value, f.Placeholder, focused, hovered)),
        MenuTitleView m when theme.Menu.TitleTemplate is { } t =>
            t(TitleState(m, highlighted, hovered, showMnemonic)),
        MenuRowView r when theme.Menu.ItemTemplate is { } t =>
            t(ItemState(r, highlighted, showMnemonic)),
        _ => null,
    };

    /// <summary>Style d'un bouton : variante posée sur la vue, sinon style nommé, sinon style standard.</summary>
    public static ButtonVariant VariantOf(ButtonView b, Theme theme) =>
        b.Variant ?? (b.StyleName is { } name ? theme.Button[name] : theme.Button[b.Style]);

    static MenuTitleState TitleState(MenuTitleView m, bool active, bool hovered, bool showMnemonic)
    {
        var (text, mnemonic) = Mnemonics.Parse(m.Menu.Title);
        return new MenuTitleState(text, mnemonic, active, hovered, showMnemonic);
    }

    static MenuItemState ItemState(MenuRowView r, bool highlighted, bool showMnemonic)
    {
        var (text, mnemonic) = Mnemonics.Parse(r.Label);
        var item = r.Item as MenuItemView;
        return new MenuItemState(text, mnemonic, item?.Gesture?.ToString(), item?.IsChecked, r.HasSubmenu,
            r.IsEnabled, highlighted && r.IsEnabled, showMnemonic);
    }
}
