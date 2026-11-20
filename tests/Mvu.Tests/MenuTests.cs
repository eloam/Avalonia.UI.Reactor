using Mvu.Primitives;
using Mvu.Tests.Probes;
using Mvu.Theming;
using Mvu.Views;

namespace Mvu.Tests;

/// <summary>Barre de menus : dessinée (souris, clavier, raccourcis, thème) ou confiée au système.</summary>
public class MenuTests
{
    [Fact]
    public void Menu_bar_spans_the_top_of_the_window_with_one_title_per_menu()
    {
        using var t = new TestHost(new MenuProbe());
        var bar = t.BoundsOf(v => v is MenuBarView);

        Assert.Equal(0, bar.Y);
        Assert.Equal(560, bar.Width);
        Assert.True(t.MenuTitleBounds("Fichier").X < t.MenuTitleBounds("Affichage").X);
        Assert.False(t.MenuIsOpen);
    }

    [Fact]
    public void Clicking_a_title_opens_its_menu_below_and_an_item_runs_its_action()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);
        var title = t.MenuTitleBounds("Fichier");

        t.ClickMenu("Fichier");
        var item = t.MenuItemBounds("Nouveau");
        Assert.True(item.Y >= title.Bottom, "The menu should open below its title.");
        Assert.True(Math.Abs(item.X - title.X) < 12, "The menu should be aligned with its title.");

        t.ClickMenuItem("Nouveau");
        Assert.Equal("nouveau", probe.Last);
        Assert.False(t.MenuIsOpen);
    }

    [Fact]
    public void Clicking_outside_closes_the_menu_without_clicking_what_is_underneath()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);

        t.ClickMenu("Fichier");
        t.ClickButton("OK");

        Assert.False(t.MenuIsOpen);
        Assert.Equal("", probe.Last);
    }

    [Fact]
    public void Clicking_the_open_title_closes_its_menu_and_hovering_another_title_switches_menus()
    {
        using var t = new TestHost(new MenuProbe());

        t.ClickMenu("Fichier");
        var other = t.MenuTitleBounds("Affichage");
        t.Ui.PointerMove(new Point(other.X + 4, other.Y + 4));
        Assert.NotNull(t.MenuEntry("Rafraîchir"));
        Assert.Null(t.MenuEntry("Nouveau"));

        t.ClickMenu("Affichage");
        Assert.False(t.MenuIsOpen);
    }

    [Fact]
    public void Disabled_item_ignores_clicks_and_its_shortcut()
    {
        var probe = new MenuProbe { CanSave = false };
        using var t = new TestHost(probe);

        t.ClickMenu("Fichier");
        t.ClickMenuItem("Enregistrer");
        Assert.True(t.MenuIsOpen);
        Assert.Equal("", probe.Last);

        t.Press(KeyCode.Escape);
        t.Press(KeyCode.S, KeyModifiers.Ctrl);
        Assert.Equal("", probe.Last);
    }

    [Fact]
    public void Menu_follows_the_state_while_it_is_open()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);
        t.ClickMenu("Fichier");
        Assert.True(t.MenuEntry("Enregistrer")!.IsEnabled);

        probe.CanSave = false;
        t.Ui.Invalidate();

        Assert.True(t.MenuIsOpen);
        Assert.False(t.MenuEntry("Enregistrer")!.IsEnabled);
    }

    [Fact]
    public void Shortcuts_run_their_action_with_menus_closed()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);

        Assert.True(t.Press(KeyCode.N, KeyModifiers.Ctrl));
        Assert.Equal("nouveau", probe.Last);
        Assert.True(t.Press(KeyCode.F5));
        Assert.Equal("f5", probe.Last);
        Assert.False(t.MenuIsOpen);
    }

    [Fact]
    public void A_plain_key_shortcut_does_not_steal_typing_but_a_command_shortcut_does_run()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);
        t.Click(v => v.Key == "field");

        Assert.False(t.Press(KeyCode.P));
        t.Type("p");
        Assert.Equal("p", probe.Draft);
        Assert.Equal("", probe.Last);

        t.Press(KeyCode.N, KeyModifiers.Ctrl);
        Assert.Equal("nouveau", probe.Last);

        t.Press(KeyCode.Escape); // le champ perd le focus : « P » redevient un raccourci
        t.Press(KeyCode.P);
        Assert.Equal("plein", probe.Last);
    }

    [Fact]
    public void Submenu_opens_on_hover_and_its_items_run()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);

        t.ClickMenu("Fichier");
        t.HoverMenuItem("Récents");
        Assert.True(t.MenuItemBounds("A.txt").X >= t.MenuItemBounds("Récents").Right - 1,
            "The submenu should open to the right of its item.");

        t.ClickMenuItem("B.txt");
        Assert.Equal("B", probe.Last);
        Assert.False(t.MenuIsOpen);
    }

    [Fact]
    public void F10_and_arrow_keys_navigate_and_enter_runs_the_highlighted_item()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);

        t.Press(KeyCode.F10);
        Assert.True(t.Ui.Menus.KeyboardMode);
        Assert.True(t.Ui.Menus.IsTitleActive(0));

        t.Press(KeyCode.Down); // ouvre « Fichier », « Nouveau » surligné
        t.Press(KeyCode.Down); // « Enregistrer »
        Assert.True(t.Ui.Menus.IsHighlighted(0, 1));
        t.Press(KeyCode.Enter);

        Assert.Equal("enregistrer", probe.Last);
        Assert.False(t.Ui.Menus.IsActive);
    }

    [Fact]
    public void Arrow_keys_skip_dividers_and_disabled_items_and_open_submenus()
    {
        var probe = new MenuProbe { CanSave = false };
        using var t = new TestHost(probe);

        t.Press(KeyCode.F10);
        t.Press(KeyCode.Down); // « Nouveau »
        t.Press(KeyCode.Down); // « Enregistrer » grisé et le séparateur sont sautés : « Récents »
        Assert.True(t.Ui.Menus.IsHighlighted(0, 3));

        t.Press(KeyCode.Right); // ouvre « Récents », « A.txt » surligné
        Assert.True(t.Ui.Menus.IsHighlighted(1, 0));
        t.Press(KeyCode.Escape); // referme le sous-menu seulement
        Assert.NotNull(t.MenuEntry("Nouveau"));
        Assert.Null(t.MenuEntry("A.txt"));
        t.Press(KeyCode.Escape);
        Assert.False(t.MenuIsOpen);
    }

    [Fact]
    public void Alt_with_an_access_key_opens_a_menu_and_a_letter_runs_an_item()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);

        t.Press(KeyCode.Alt, KeyModifiers.Alt);
        t.Press(KeyCode.A, KeyModifiers.Alt);
        t.Release(KeyCode.Alt);
        Assert.NotNull(t.MenuEntry("Rafraîchir"));
        Assert.True(t.Ui.Menus.ShowMnemonics);

        t.Press(KeyCode.R);
        Assert.Equal("f5", probe.Last);
    }

    [Fact]
    public void Alt_pressed_and_released_alone_toggles_keyboard_mode()
    {
        using var t = new TestHost(new MenuProbe());

        t.Press(KeyCode.Alt, KeyModifiers.Alt);
        Assert.True(t.Release(KeyCode.Alt));
        Assert.True(t.Ui.Menus.KeyboardMode);

        t.Press(KeyCode.Alt, KeyModifiers.Alt);
        t.Release(KeyCode.Alt);
        Assert.False(t.Ui.Menus.IsActive);

        // Alt Gr (Ctrl + Alt) n'active pas la barre.
        t.Press(KeyCode.Alt, KeyModifiers.Ctrl | KeyModifiers.Alt);
        Assert.False(t.Release(KeyCode.Alt));
    }

    [Fact]
    public void Characters_of_keys_taken_by_the_menus_are_not_typed()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);
        t.Click(v => v.Key == "field");

        t.Press(KeyCode.F, KeyModifiers.Alt);
        t.Ui.TextInput("f"); // caractère que la plateforme envoie avec la touche
        Assert.Equal("", probe.Draft);
        Assert.NotNull(t.MenuEntry("Nouveau"));
    }

    [Fact]
    public void Checked_item_bound_to_a_state_toggles_it()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);

        t.ClickMenu("Affichage");
        Assert.False(t.MenuEntry("Mode sombre")!.IsChecked);
        t.ClickMenuItem("Mode sombre");
        Assert.True(probe.Dark);

        t.ClickMenu("Affichage");
        Assert.True(t.MenuEntry("Mode sombre")!.IsChecked);
    }

    [Fact]
    public void Losing_window_focus_closes_the_menus()
    {
        using var t = new TestHost(new MenuProbe());
        t.ClickMenu("Fichier");

        t.Ui.WindowDeactivated();

        Assert.False(t.MenuIsOpen);
    }

    [Fact]
    public void Native_menu_bar_receives_the_menus_and_takes_no_space_in_the_window()
    {
        var probe = new MenuProbe();
        var native = new FakeNativeMenuBar();
        using var t = new TestHost(probe, nativeMenuBar: native);

        Assert.Equal(2, native.Bar!.Menus.Count);
        Assert.Equal(0, t.BoundsOf(v => v is MenuBarView).Height);
        Assert.Equal(0, t.BoundsOfKey("last").Y); // ni hauteur ni espacement
        Assert.Empty(t.Views.OfType<MenuTitleView>());

        native.Choose(menu: 0, item: 0);
        Assert.Equal("nouveau", probe.Last);

        t.Press(KeyCode.S, KeyModifiers.Ctrl); // les raccourcis restent gérés par le framework
        Assert.Equal("enregistrer", probe.Last);
        t.Press(KeyCode.F10); // F10 et Alt appartiennent au système
        Assert.False(t.Ui.Menus.IsActive);
    }

    [Fact]
    public void Native_menu_bar_is_updated_when_the_state_changes()
    {
        var probe = new MenuProbe();
        var native = new FakeNativeMenuBar();
        using var t = new TestHost(probe, nativeMenuBar: native);
        int updates = native.Updates;

        probe.CanSave = false;
        t.Ui.Invalidate();

        Assert.True(native.Updates > updates);
        Assert.False(((MenuItemView)native.Bar!.Menus[0].Items[1]).IsEnabled);
    }

    [Fact]
    public void Menu_colors_come_from_the_theme()
    {
        var red = Color.Hex("#FF0000");
        var blue = Color.Hex("#0000FF");
        var theme = Theme.Light.WithMenu(m => m with { BarBackground = blue, HighlightBackground = red });
        using var t = new TestHost(new MenuProbe(), themes: theme);

        var bar = t.BoundsOf(v => v is MenuBarView);
        Assert.Equal(blue, t.PixelAt(bar.Right - 4, bar.Y + 4));

        t.ClickMenu("Fichier");
        t.HoverMenuItem("Nouveau");
        var row = t.MenuItemBounds("Nouveau");
        Assert.Equal(red, t.PixelAt(row.X + 3, row.Y + row.Height / 2));
    }

    [Fact]
    public void Item_template_replaces_the_standard_drawing_and_follows_the_highlight()
    {
        var red = Color.Hex("#FF0000");
        var green = Color.Hex("#00FF00");
        var theme = Theme.Light.WithMenu(m => m with
        {
            ItemTemplate = s => Text(s.Text).Padding(6).Background(s.Highlighted ? red : green),
        });
        using var t = new TestHost(new MenuProbe(), themes: theme);

        t.ClickMenu("Fichier");
        t.HoverMenuItem("Nouveau");

        var hovered = t.MenuItemBounds("Nouveau");
        var other = t.MenuItemBounds("Quitter");
        Assert.Equal(red, t.PixelAt(hovered.X + 2, hovered.Y + 2));
        Assert.Equal(green, t.PixelAt(other.X + 2, other.Y + 2));
    }
}
