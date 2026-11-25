// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using MartletUI.Menus;
using MartletUI.Tests.Probes;
using MartletUI.Theming;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// Menu bar declared in the <c>Body</c>, rendered by the Avalonia <c>Menu</c> (the headless platform has no system menu
/// bar): items, access keys, bound check marks, disabled items, shortcuts with the menus closed.
/// </summary>
public class MenuTests
{
    private static MartletMenuItem ItemAt(MartletMenuBar bar, params int[] path)
    {
        var item = (MartletMenuItem)bar.Items[path[0]]!;
        foreach (int i in path[1..])
            item = (MartletMenuItem)item.Items[i]!;
        return item;
    }

    [AvaloniaFact]
    public void The_bar_is_drawn_across_the_top_of_the_window_with_access_keys()
    {
        using var t = new MartletTestHost(new MenuProbe());
        MartletMenuBar bar = t.Controls.OfType<MartletMenuBar>().Single();

        Assert.False(bar.UsesSystemBar);
        Assert.Equal(new Rect(0, 0, 400, bar.Bounds.Height), t.BoundsOf(bar));
        Assert.Equal(["_Fichier", "_Affichage"], bar.Items.Cast<MenuItem>().Select(m => m.Header));
        Assert.IsType<Separator>(ItemAt(bar, 0).Items[2]);
        Assert.Equal("rapport.txt", ItemAt(bar, 0, 3, 0).Header);
    }

    [AvaloniaFact]
    public void Choosing_an_item_runs_its_action()
    {
        var probe = new MenuProbe();
        using var t = new MartletTestHost(probe);

        ItemAt(t.Controls.OfType<MartletMenuBar>().Single(), 0, 3, 0).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.MenuItem.ClickEvent));
        t.Settle();

        Assert.Equal("rapport", probe.Last);
    }

    [AvaloniaFact]
    public void Shortcuts_run_their_action_with_menus_closed_and_a_disabled_item_does_nothing()
    {
        var probe = new MenuProbe();
        using var t = new MartletTestHost(probe);

        t.Press(Key.N, PhysicalKey.N, MartletTestHost.Command);
        Assert.Equal("nouveau", probe.Last);

        probe.CanSave = false;
        t.Settle();
        t.Press(Key.S, PhysicalKey.S, MartletTestHost.Command);
        Assert.Equal("nouveau", probe.Last);
        Assert.False(ItemAt(t.Controls.OfType<MartletMenuBar>().Single(), 0, 1).IsEnabled);
    }

    [AvaloniaFact]
    public void A_checked_item_follows_its_state_in_the_same_menu_item()
    {
        var probe = new MenuProbe();
        using var t = new MartletTestHost(probe);
        MartletMenuBar bar = t.Controls.OfType<MartletMenuBar>().Single();
        MartletMenuItem counter = ItemAt(bar, 1, 0);
        Assert.Equal(MenuItemToggleType.CheckBox, counter.ToggleType);
        Assert.False(counter.IsChecked);

        t.Press(Key.F5, PhysicalKey.F5);

        Assert.True(probe.ShowCounter);
        Assert.Same(counter, ItemAt(bar, 1, 0));
        Assert.True(counter.IsChecked);
    }

    [AvaloniaFact]
    public void A_plain_letter_shortcut_does_not_steal_typing_but_a_command_shortcut_does_run()
    {
        var probe = new MenuProbe();
        using var t = new MartletTestHost(probe);
        t.Click(t.Field);

        t.Press(Key.P, PhysicalKey.P);
        t.Type("p");
        Assert.Equal("p", probe.Draft);
        Assert.Equal("", probe.Last);

        t.Press(Key.N, PhysicalKey.N, MartletTestHost.Command);
        Assert.Equal("nouveau", probe.Last);
    }

    [AvaloniaFact]
    public void A_field_has_a_context_menu_labelled_by_its_theme()
    {
        Theme french = Theme.Light.WithTextField(f => f with { EditMenu = new("Couper", "Copier", "Coller") });
        using var t = new MartletTestHost(new ViewProbe(() => VStack(TextField("texte", onChange: null))), themes: french);

        MenuFlyout menu = Assert.IsType<MenuFlyout>(t.Field.ContextFlyout);

        Assert.Equal(["Couper", "Copier", "Coller"], menu.Items.Cast<MenuItem>().Select(m => m.Header));
    }

    [Theory]
    [InlineData("&Fichier", "_Fichier")]
    [InlineData("Enregistrer && quitter", "Enregistrer & quitter")]
    [InlineData("mon_fichier", "mon__fichier")]
    [InlineData("&Plein écran", "_Plein écran")]
    public void Access_keys_are_written_the_Avalonia_way(string label, string header) =>
        Assert.Equal(header, MenuSync.AccessText(label));

    [AvaloniaFact]
    public void The_bar_takes_its_colors_and_margins_from_the_theme()
    {
        Theme theme = Theme.Light.WithMenu(m => m with { BarBackground = "#3366CC", BarBorderWidth = 2, BarPadding = new(6) });
        using var t = new MartletTestHost(new MenuProbe(), themes: theme);
        MartletMenuBar bar = t.Controls.OfType<MartletMenuBar>().Single();

        Assert.Equal(Primitives.Color.Hex("#3366CC").ToAvaloniaColor(), bar.Background.ColorOf());
        Assert.Equal(new Thickness(0, 0, 0, 2), bar.BorderThickness);
        Assert.Equal(new Thickness(6), bar.Padding);
    }
}
