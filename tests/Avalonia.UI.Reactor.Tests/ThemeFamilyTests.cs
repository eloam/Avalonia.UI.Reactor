using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// Familles de thèmes (clair et sombre) et thème déclaré par la vue : la famille par défaut suit le système, une branche
/// choisit sa famille ou son mode, un thème exact garde sa variante (catalogue des tests : Standard et Sable).
/// </summary>
public class ThemeFamilyTests
{
    static ReactorTestHost Show(Func<View> body) => new(new ViewProbe(body), themes: TestThemes.Create());

    static Color TextOf(Theme theme) => theme.Palette.Text.ToAvaloniaColor();

    [AvaloniaFact]
    public void The_application_follows_the_system_with_the_first_family()
    {
        using var t = Show(() => Text("x"));
        Assert.Equal(Theme.Light.Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());

        t.Host.Themes.SystemScheme = ColorScheme.Dark;
        t.Settle();

        Assert.Equal(Theme.Dark.Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());
    }

    [AvaloniaFact]
    public void A_family_chosen_by_name_follows_the_system()
    {
        using var t = Show(() => Text("x").Theme("Sable"));
        Assert.Equal(TextOf(TestThemes.Sable), t.TextShowing("x").Foreground.ColorOf());

        t.Host.Themes.SystemScheme = ColorScheme.Dark;
        t.Settle();

        Assert.Equal(TextOf(TestThemes.SableNight), t.TextShowing("x").Foreground.ColorOf());
    }

    [AvaloniaFact]
    public void A_single_view_or_a_whole_component_can_use_another_theme()
    {
        var child = new ViewProbe(() => Text("composant"));
        using var t = Show(() => VStack(Text("standard"), Text("vue").Theme("Sable"), child.Theme("Sable")));

        Assert.Equal(TextOf(Theme.Light), t.TextShowing("standard").Foreground.ColorOf());
        Assert.Equal(TextOf(TestThemes.Sable), t.TextShowing("vue").Foreground.ColorOf());
        Assert.Equal(TextOf(TestThemes.Sable), t.TextShowing("composant").Foreground.ColorOf());
    }

    [AvaloniaFact]
    public void ColorScheme_picks_the_variant_of_the_inherited_family()
    {
        using var t = Show(() => VStack(Text("clair"), Text("nuit").ColorScheme(ColorScheme.Dark)).Theme("Sable"));

        Assert.Equal(TextOf(TestThemes.Sable), t.TextShowing("clair").Foreground.ColorOf());
        Assert.Equal(TextOf(TestThemes.SableNight), t.TextShowing("nuit").Foreground.ColorOf());
    }

    [AvaloniaFact]
    public void An_exact_theme_keeps_its_variant_and_passes_its_mode_down()
    {
        using var t = Show(() => VStack(Text("nuit"), Text("standard").Theme("Standard")).Theme(TestThemes.SableNight));

        Assert.Equal(TextOf(TestThemes.SableNight), t.TextShowing("nuit").Foreground.ColorOf()); // même si le système est clair
        Assert.Equal(TextOf(Theme.Dark), t.TextShowing("standard").Foreground.ColorOf());       // Standard, sombre comme son parent
    }

    [AvaloniaFact]
    public void An_unknown_theme_name_keeps_the_inherited_theme()
    {
        using var t = Show(() => VStack(Text("x").Theme("absent")));

        Assert.Equal(TextOf(Theme.Light), t.TextShowing("x").Foreground.ColorOf());
    }

    [Fact]
    public void The_catalog_groups_themes_into_families()
    {
        var pair = new Themes(TestThemes.Sable, TestThemes.SableNight); // sans nom : une famille nommée d'après le thème clair
        Assert.Single(pair.Families);
        Assert.Equal("Sable", pair.DefaultFamily.Name);
        Assert.True(pair.DefaultFamily.FollowsSystem);
        Assert.Same(TestThemes.SableNight, pair["sable NUIT"]);

        Assert.Equal("Standard", Themes.Default.DefaultFamily.Name);
        Assert.Equal("Test", ((Themes)Theme.Light.Named("Test")).DefaultFamily.Name); // un seul thème : une famille à une variante
        Assert.Throws<ArgumentException>(() => new Themes { { "Deux clairs", Theme.Light, TestThemes.Sable } });
    }
}
