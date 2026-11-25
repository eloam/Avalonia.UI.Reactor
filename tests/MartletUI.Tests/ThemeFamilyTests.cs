// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;
using Avalonia.Media;
using MartletUI.Tests.Probes;
using MartletUI.Theming;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// Theme families (light and dark) and themes declared by the view: the default family follows the system, a branch
/// chooses its family or its mode, an exact theme keeps its variant (test catalog: Standard and Sable).
/// </summary>
public class ThemeFamilyTests
{
    private static MartletTestHost Show(Func<View> body) => new(new ViewProbe(body), themes: TestThemes.Create());

    private static Color TextOf(Theme theme) => theme.Palette.Text.ToAvaloniaColor();

    [AvaloniaFact]
    public void The_application_follows_the_system_with_the_first_family()
    {
        using MartletTestHost t = Show(() => Text("x"));
        Assert.Equal(Theme.Light.Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());

        t.Host.Themes.SystemScheme = ColorScheme.Dark;
        t.Settle();

        Assert.Equal(Theme.Dark.Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());
    }

    [AvaloniaFact]
    public void A_family_chosen_by_name_follows_the_system()
    {
        using MartletTestHost t = Show(() => Text("x").Theme("Sable"));
        Assert.Equal(TextOf(TestThemes.Sable), t.TextShowing("x").Foreground.ColorOf());

        t.Host.Themes.SystemScheme = ColorScheme.Dark;
        t.Settle();

        Assert.Equal(TextOf(TestThemes.SableNight), t.TextShowing("x").Foreground.ColorOf());
    }

    [AvaloniaFact]
    public void A_single_view_or_a_whole_component_can_use_another_theme()
    {
        var child = new ViewProbe(() => Text("composant"));
        using MartletTestHost t = Show(() => VStack(Text("standard"), Text("vue").Theme("Sable"), child.Theme("Sable")));

        Assert.Equal(TextOf(Theme.Light), t.TextShowing("standard").Foreground.ColorOf());
        Assert.Equal(TextOf(TestThemes.Sable), t.TextShowing("vue").Foreground.ColorOf());
        Assert.Equal(TextOf(TestThemes.Sable), t.TextShowing("composant").Foreground.ColorOf());
    }

    [AvaloniaFact]
    public void ColorScheme_picks_the_variant_of_the_inherited_family()
    {
        using MartletTestHost t = Show(() => VStack(Text("clair"), Text("nuit").ColorScheme(ColorScheme.Dark)).Theme("Sable"));

        Assert.Equal(TextOf(TestThemes.Sable), t.TextShowing("clair").Foreground.ColorOf());
        Assert.Equal(TextOf(TestThemes.SableNight), t.TextShowing("nuit").Foreground.ColorOf());
    }

    [AvaloniaFact]
    public void An_exact_theme_keeps_its_variant_and_passes_its_mode_down()
    {
        using MartletTestHost t = Show(() => VStack(Text("nuit"), Text("standard").Theme("Standard")).Theme(TestThemes.SableNight));

        // Even though the system is light.
        Assert.Equal(TextOf(TestThemes.SableNight), t.TextShowing("nuit").Foreground.ColorOf());
        // Standard, dark like its parent.
        Assert.Equal(TextOf(Theme.Dark), t.TextShowing("standard").Foreground.ColorOf());
    }

    [AvaloniaFact]
    public void An_unknown_theme_name_keeps_the_inherited_theme()
    {
        using MartletTestHost t = Show(() => VStack(Text("x").Theme("absent")));

        Assert.Equal(TextOf(Theme.Light), t.TextShowing("x").Foreground.ColorOf());
    }

    [Fact]
    public void The_catalog_groups_themes_into_families()
    {
        // Without a name: one family, named after the light theme.
        var pair = new Themes(TestThemes.Sable, TestThemes.SableNight);
        Assert.Single(pair.Families);
        Assert.Equal("Sable", pair.DefaultFamily.Name);
        Assert.True(pair.DefaultFamily.FollowsSystem);
        Assert.Same(TestThemes.SableNight, pair["sable NUIT"]);

        Assert.Equal("Standard", Themes.Default.DefaultFamily.Name);
        // A single theme: a family with one variant.
        Assert.Equal("Test", ((Themes)Theme.Light.Named("Test")).DefaultFamily.Name);
        Assert.Throws<ArgumentException>(() => new Themes { { "Deux clairs", Theme.Light, TestThemes.Sable } });
    }
}
