// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Text;
using Avalonia.Controls;
using MartletUI.Hosting;
using MartletUI.Views;

namespace MartletUI.Menus;

/// <summary>
/// Applies the description of the menus (titles, items, submenus, separators) to Avalonia menu items, reusing those
/// that already hold the same place, so that an open menu follows the state (check marks, disabled items) without
/// closing.
/// </summary>
internal static class MenuSync
{
    /// <summary>
    /// Applies menu entries to the items of the bar drawn in the window, or of one of its menus.
    /// </summary>
    /// <param name="items">The Avalonia items.</param>
    /// <param name="entries">The menu views.</param>
    /// <param name="run">The function that runs the action of an item, then updates the interface.</param>
    /// <exception cref="NotSupportedException">An entry is neither a menu item, a menu nor a divider.</exception>
    public static void Apply(ItemCollection items, IReadOnlyList<View> entries, Action<Action?> run)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            object wanted = entries[i] switch
            {
                DividerView => items.ElementAtOrDefault(i) as Separator ?? new Separator(),
                MenuView menu => Item(items.ElementAtOrDefault(i), run, item =>
                {
                    item.Header = AccessText(menu.Title);
                    item.Entry = null;
                    item.InputGesture = null;
                    item.ToggleType = MenuItemToggleType.None;
                    item.IsEnabled = true;
                    Apply(item.Items, menu.Items, run);
                }),
                MenuItemView entry => Item(items.ElementAtOrDefault(i), run, item =>
                {
                    item.Header = AccessText(entry.Label);
                    item.Entry = entry;
                    item.InputGesture = entry.Gesture is { } gesture ? KeyMap.ToGesture(gesture) : null;
                    item.ToggleType = entry.IsChecked is null ? MenuItemToggleType.None : MenuItemToggleType.CheckBox;
                    item.IsChecked = entry.IsChecked == true;
                    item.IsEnabled = entry.IsEnabled;
                    item.Items.Clear();
                }),
                var other => throw Unsupported(other),
            };
            if (i >= items.Count)
            {
                items.Add(wanted);
            }
            else if (!ReferenceEquals(items[i], wanted))
            {
                items[i] = wanted;
            }
        }

        while (items.Count > entries.Count)
        {
            items.RemoveAt(items.Count - 1);
        }
    }

    /// <summary>
    /// Applies menu entries to the items of the system menu bar, or of one of its menus.
    /// </summary>
    /// <param name="items">The native menu items.</param>
    /// <param name="entries">The menu views.</param>
    /// <param name="run">The function that runs the action of an item, then updates the interface.</param>
    /// <exception cref="NotSupportedException">An entry is neither a menu item, a menu nor a divider.</exception>
    public static void Apply(IList<NativeMenuItemBase> items, IReadOnlyList<View> entries, Action<Action?> run)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            NativeMenuItemBase wanted = entries[i] switch
            {
                DividerView => items.ElementAtOrDefault(i) as NativeMenuItemSeparator ?? new NativeMenuItemSeparator(),
                MenuView menu => NativeItem(items.ElementAtOrDefault(i), run, item =>
                {
                    item.Header = Mnemonics.Parse(menu.Title).Text;
                    item.Entry = null;
                    item.Gesture = null;
                    item.ToggleType = MenuItemToggleType.None;
                    item.IsEnabled = true;
                    item.Menu ??= [];
                    Apply(item.Menu.Items, menu.Items, run);
                }),
                MenuItemView entry => NativeItem(items.ElementAtOrDefault(i), run, item =>
                {
                    item.Header = Mnemonics.Parse(entry.Label).Text;
                    item.Entry = entry;
                    item.Gesture = entry.Gesture is { } gesture ? KeyMap.ToGesture(gesture) : null;
                    item.ToggleType = entry.IsChecked is null ? MenuItemToggleType.None : MenuItemToggleType.CheckBox;
                    item.IsChecked = entry.IsChecked == true;
                    item.IsEnabled = entry.IsEnabled;
                    item.Menu = null;
                }),
                var other => throw Unsupported(other),
            };
            if (i >= items.Count)
            {
                items.Add(wanted);
            }
            else if (!ReferenceEquals(items[i], wanted))
            {
                items[i] = wanted;
            }
        }

        while (items.Count > entries.Count)
        {
            items.RemoveAt(items.Count - 1);
        }
    }

    /// <summary>
    /// Writes a label with its access key the Avalonia way: <c>"&amp;File"</c> becomes <c>"_File"</c>,
    /// <c>"&amp;&amp;"</c> an ampersand, and an underscore of the label is doubled to stay a character.
    /// </summary>
    /// <param name="label">The label, with <c>&amp;</c> before the access key.</param>
    /// <returns>The label in the access text syntax of Avalonia.</returns>
    public static string AccessText(string label)
    {
        (string text, int index) = Mnemonics.Parse(label);
        var result = new StringBuilder(text.Length + 1);
        for (int i = 0; i < text.Length; i++)
        {
            if (i == index)
            {
                result.Append('_');
            }

            result.Append(text[i]);
            if (text[i] == '_')
            {
                result.Append('_');
            }
        }

        return result.ToString();
    }

    private static MartletMenuItem Item(object? existing, Action<Action?> run, Action<MartletMenuItem> apply)
    {
        if (existing is not MartletMenuItem item)
        {
            item = new MartletMenuItem();
            MartletMenuItem created = item;
            created.Click += (_, _) => run(created.Entry is { IsEnabled: true } entry ? entry.Action : null);
        }

        apply(item);
        return item;
    }

    private static MartletNativeMenuItem NativeItem(NativeMenuItemBase? existing, Action<Action?> run, Action<MartletNativeMenuItem> apply)
    {
        if (existing is not MartletNativeMenuItem item)
        {
            item = new MartletNativeMenuItem();
            MartletNativeMenuItem created = item;
            created.Click += (_, _) => run(created.Entry is { IsEnabled: true } entry ? entry.Action : null);
        }

        apply(item);
        return item;
    }

    private static NotSupportedException Unsupported(View entry) =>
        new($"A menu cannot contain a '{entry.GetType().Name}'. Use 'MenuItem', 'Menu' or 'Divider'.");
}
