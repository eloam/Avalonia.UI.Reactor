namespace Mvu.Views;

/// <summary>
/// Ce que reçoit un modèle d'élément de menu (<c>MenuTheme.ItemTemplate</c>) : texte sans <c>&amp;</c>,
/// lettre d'accès, raccourci affiché, coche, sous-menu, et état (surligné, désactivé, lettres à souligner).
/// </summary>
public sealed record MenuItemState(
    string Text, int MnemonicIndex, string? ShortcutText, bool? Checked, bool HasSubmenu,
    bool Enabled, bool Highlighted, bool ShowMnemonic);
