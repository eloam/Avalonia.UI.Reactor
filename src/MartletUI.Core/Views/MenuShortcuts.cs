// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;

namespace MartletUI.Views;

/// <summary>
/// Finds the shortcuts of a menu bar, active even when the menus are closed.
/// </summary>
internal static class MenuShortcuts
{
    /// <summary>
    /// Returns the item of the bar (menus and submenus included) that answers a key.
    /// </summary>
    /// <remarks>
    /// While text is being typed, only shortcuts with Ctrl, Alt, Cmd or a function key apply: a plain letter remains a
    /// typed letter.
    /// </remarks>
    /// <param name="bar">The menu bar.</param>
    /// <param name="key">The pressed key.</param>
    /// <param name="modifiers">The modifier keys held.</param>
    /// <param name="editingText">A value indicating whether a text field has the focus.</param>
    /// <returns>The item whose shortcut matches, or <see langword="null"/>.</returns>
    public static MenuItemView? Find(MenuBarView bar, KeyCode key, KeyModifiers modifiers, bool editingText)
    {
        if (Find(bar.Menus, key, modifiers) is not (var item, var gesture))
        {
            return null;
        }

        return editingText && !gesture.IsCommand ? null : item;
    }

    private static (MenuItemView Item, KeyGesture Gesture)? Find(IEnumerable<View> items, KeyCode key, KeyModifiers modifiers)
    {
        foreach (View item in items)
        {
            (MenuItemView Item, KeyGesture Gesture)? found = item switch
            {
                MenuItemView { Gesture: { } gesture } menuItem when gesture.Matches(key, modifiers) => (menuItem, gesture),
                MenuView menu => Find(menu.Items, key, modifiers),
                _ => null,
            };
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
