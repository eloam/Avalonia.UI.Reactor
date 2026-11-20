using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

/// <summary>Raccourcis d'une barre de menus, actifs même menus fermés, quel que soit le moteur qui l'affiche.</summary>
internal static class MenuShortcuts
{
    /// <summary>
    /// Élément de la barre (menus et sous-menus) qui répond à cette touche. Pendant une saisie de texte, seuls les
    /// raccourcis avec Ctrl, Alt, Cmd ou une touche de fonction s'appliquent : une simple lettre reste une lettre tapée.
    /// </summary>
    public static MenuItemView? Find(MenuBarView bar, KeyCode key, KeyModifiers modifiers, bool editingText)
    {
        if (Find(bar.Menus, key, modifiers) is not (var item, var gesture)) return null;
        return editingText && !gesture.IsCommand ? null : item;
    }

    static (MenuItemView Item, KeyGesture Gesture)? Find(IEnumerable<View> items, KeyCode key, KeyModifiers modifiers)
    {
        foreach (var item in items)
        {
            var found = item switch
            {
                MenuItemView { Gesture: { } g } i when g.Matches(key, modifiers) => (i, g),
                MenuView m => Find(m.Items, key, modifiers),
                _ => null,
            };
            if (found is not null) return found;
        }
        return null;
    }
}
