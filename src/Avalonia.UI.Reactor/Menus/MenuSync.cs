using System.Text;
using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Menus;

using Avalonia.UI.Reactor.Hosting;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Applique la description des menus (titres, éléments, sous-menus, séparateurs) aux éléments d'Avalonia, en reprenant
/// ceux qui occupent déjà la même place : un menu ouvert suit l'état (coches, éléments grisés) sans se refermer.
/// </summary>
internal static class MenuSync
{
    /// <summary>Éléments de la barre dessinée dans la fenêtre (ou d'un de ses menus).</summary>
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
            if (i < items.Count) { if (!ReferenceEquals(items[i], wanted)) items[i] = wanted; }
            else items.Add(wanted);
        }
        while (items.Count > entries.Count) items.RemoveAt(items.Count - 1);
    }

    /// <summary>Éléments de la barre du système (ou d'un de ses menus).</summary>
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
                    item.Menu ??= new NativeMenu();
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
            if (i < items.Count) { if (!ReferenceEquals(items[i], wanted)) items[i] = wanted; }
            else items.Add(wanted);
        }
        while (items.Count > entries.Count) items.RemoveAt(items.Count - 1);
    }

    /// <summary>
    /// Libellé avec sa lettre d'accès à la manière d'Avalonia : <c>"&amp;Fichier"</c> devient <c>"_Fichier"</c>,
    /// <c>"&amp;&amp;"</c> un « &amp; », et un « _ » du libellé est doublé pour rester un caractère.
    /// </summary>
    internal static string AccessText(string label)
    {
        var (text, index) = Mnemonics.Parse(label);
        var result = new StringBuilder(text.Length + 1);
        for (int i = 0; i < text.Length; i++)
        {
            if (i == index) result.Append('_');
            result.Append(text[i]);
            if (text[i] == '_') result.Append('_');
        }
        return result.ToString();
    }

    static ReactorMenuItem Item(object? existing, Action<Action?> run, Action<ReactorMenuItem> apply)
    {
        if (existing is not ReactorMenuItem item)
        {
            item = new ReactorMenuItem();
            var created = item;
            created.Click += (_, _) => run(created.Entry is { IsEnabled: true } entry ? entry.Action : null);
        }
        apply(item);
        return item;
    }

    static ReactorNativeMenuItem NativeItem(NativeMenuItemBase? existing, Action<Action?> run, Action<ReactorNativeMenuItem> apply)
    {
        if (existing is not ReactorNativeMenuItem item)
        {
            item = new ReactorNativeMenuItem();
            var created = item;
            created.Click += (_, _) => run(created.Entry is { IsEnabled: true } entry ? entry.Action : null);
        }
        apply(item);
        return item;
    }

    static NotSupportedException Unsupported(View entry) =>
        new($"A menu cannot contain a '{entry.GetType().Name}'. Use 'MenuItem', 'Menu' or 'Divider'.");
}
