using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Headless.Tests.Probes;
using Avalonia.UI.Reactor.Menus;
using Avalonia.UI.Reactor.Theming;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// Barre de menus déclarée dans le <c>Body</c>, rendue par le <c>Menu</c> d'Avalonia (la plateforme sans fenêtre n'a pas
/// de barre du système) : éléments, lettres d'accès, coches liées, éléments grisés, raccourcis menus fermés.
/// </summary>
public class MenuTests
{
    static ReactorMenuItem ItemAt(ReactorMenuBar bar, params int[] path)
    {
        var item = (ReactorMenuItem)bar.Items[path[0]]!;
        foreach (int i in path[1..]) item = (ReactorMenuItem)item.Items[i]!;
        return item;
    }

    [AvaloniaFact]
    public void The_bar_is_drawn_across_the_top_of_the_window_with_access_keys()
    {
        using var t = new ReactorTestHost(new MenuProbe());
        var bar = t.Controls.OfType<ReactorMenuBar>().Single();

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
        using var t = new ReactorTestHost(probe);

        ItemAt(t.Controls.OfType<ReactorMenuBar>().Single(), 0, 3, 0).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.MenuItem.ClickEvent));
        t.Settle();

        Assert.Equal("rapport", probe.Last);
    }

    [AvaloniaFact]
    public void Shortcuts_run_their_action_with_menus_closed_and_a_disabled_item_does_nothing()
    {
        var probe = new MenuProbe();
        using var t = new ReactorTestHost(probe);

        t.Press(Key.N, PhysicalKey.N, ReactorTestHost.Command);
        Assert.Equal("nouveau", probe.Last);

        probe.CanSave = false;
        t.Settle();
        t.Press(Key.S, PhysicalKey.S, ReactorTestHost.Command);
        Assert.Equal("nouveau", probe.Last);
        Assert.False(ItemAt(t.Controls.OfType<ReactorMenuBar>().Single(), 0, 1).IsEnabled);
    }

    [AvaloniaFact]
    public void A_checked_item_follows_its_state_in_the_same_menu_item()
    {
        var probe = new MenuProbe();
        using var t = new ReactorTestHost(probe);
        var bar = t.Controls.OfType<ReactorMenuBar>().Single();
        var counter = ItemAt(bar, 1, 0);
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
        using var t = new ReactorTestHost(probe);
        t.Click(t.Field);

        t.Press(Key.P, PhysicalKey.P);
        t.Type("p");
        Assert.Equal("p", probe.Draft);
        Assert.Equal("", probe.Last);

        t.Press(Key.N, PhysicalKey.N, ReactorTestHost.Command);
        Assert.Equal("nouveau", probe.Last);
    }

    [AvaloniaFact]
    public void A_field_has_a_context_menu_labelled_by_its_theme()
    {
        var french = Theme.Light.WithTextField(f => f with { EditMenu = new("Couper", "Copier", "Coller") });
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(TextField("texte", onChange: null))), themes: french);

        var menu = Assert.IsType<MenuFlyout>(t.Field.ContextFlyout);

        Assert.Equal(["Couper", "Copier", "Coller"], menu.Items.Cast<MenuItem>().Select(m => m.Header));
    }

    [Theory]
    [InlineData("&Fichier", "_Fichier")]
    [InlineData("Enregistrer && quitter", "Enregistrer & quitter")]
    [InlineData("mon_fichier", "mon__fichier")]
    [InlineData("&Plein écran", "_Plein écran")]
    public void Access_keys_are_written_the_Avalonia_way(string label, string header) =>
        Assert.Equal(header, MenuSync.AccessText(label));
}
