using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>Thèmes nommés, choix du thème, couleurs / styles ajoutés par nom, valeurs venues du thème.</summary>
public class ThemingTests
{
    static readonly Theme Custom = Theme.Light
        .Named("Test")
        .WithColor("success", "#00AA00")
        .WithTextStyle("caption", size: 10)
        .WithButtonStyle("success", new ButtonVariant(Background: "success", Foreground: ColorRole.OnAccent))
        .WithSpacing(20)
        with { DividerThickness = 3 };

    [Fact]
    public void Color_added_by_name_is_used_and_unknown_names_are_visible()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            Canvas(d => d.FillRect(d.Bounds, "success")).Size(20, 20),
            Canvas(d => d.FillRect(d.Bounds, "inconnue")).Size(20, 20)).Spacing(0)), themes: Custom);

        Assert.Equal(Color.Hex("#00AA00"), t.PixelAt(10, 10));
        Assert.Equal(Palette.Missing, t.PixelAt(10, 30));
    }

    [Fact]
    public void Text_style_added_by_name_applies()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            Text("normal").Key("normal"),
            Text("petit").Font("caption").Key("caption"))), themes: Custom);

        Assert.True(t.BoundsOfKey("caption").Height < t.BoundsOfKey("normal").Height);
    }

    [Fact]
    public void Button_style_added_by_name_applies()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(Button("OK").Style("success").Size(60, 30))), themes: Custom);
        Assert.Equal(Color.Hex("#00AA00"), t.PixelAt(5, 5));
    }

    [Fact]
    public void Default_stack_spacing_and_divider_come_from_the_theme()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            Box().Size(10, 10).Key("a"),
            Box().Size(10, 10).Key("b"),
            Divider().Key("trait"))), themes: Custom);

        Assert.Equal(30, t.BoundsOfKey("b").Y);            // 10 + espacement 20
        Assert.Equal(3, t.BoundsOfKey("trait").Height);    // épaisseur du thème
    }

    // ── Familles et thème déclaré par la vue ──

    static readonly Theme Sable = Theme.Light.Named("Sable")
        .WithColor(ColorRole.Page, "#FBF7F2").WithColor(ColorRole.Accent, "#C2410C");
    static readonly Theme SableNight = Theme.Dark.Named("Sable nuit")
        .WithColor(ColorRole.Page, "#1A1512").WithColor(ColorRole.Accent, "#F97316");

    static Themes Catalog() => new()
    {
        { "Standard", Theme.Light, Theme.Dark },
        { "Sable", Sable, SableNight },
    };

    /// <summary>Petit carré peint avec un rôle du thème : sa couleur dit quel thème s'applique à cet endroit.</summary>
    static BoxView Swatch(ColorRole role) => Box().Size(10, 10).Background(role);

    [Fact]
    public void App_follows_the_system_with_the_first_family()
    {
        using var t = new TestHost(new ViewProbe(() => Text("x")), themes: Catalog());
        Assert.Equal(Theme.Light.Palette.Page, t.Ui.ClearColor);

        t.Ui.SystemScheme = ColorScheme.Dark;
        t.Ui.Invalidate();
        Assert.Equal(Theme.Dark.Palette.Page, t.Ui.ClearColor);
    }

    [Fact]
    public void A_family_chosen_by_name_follows_the_system()
    {
        using var t = new TestHost(new ViewProbe(() => Text("x").Theme("Sable")), themes: Catalog());
        Assert.Equal("Sable", t.Ui.CurrentTheme.Name);

        t.Ui.SystemScheme = ColorScheme.Dark;
        t.Ui.Invalidate();
        Assert.Equal("Sable nuit", t.Ui.CurrentTheme.Name);
    }

    [Fact]
    public void A_single_view_or_component_can_use_another_theme()
    {
        var child = new ViewProbe(() => Swatch(ColorRole.Accent));
        using var t = new TestHost(new ViewProbe(() => VStack(
            Swatch(ColorRole.Accent),
            Swatch(ColorRole.Accent).Theme("Sable"),     // une vue…
            child.Theme("Sable")).Spacing(0)), themes: Catalog());   // …ou un composant entier

        Assert.Equal(Theme.Light.Palette.Accent, t.PixelAt(5, 5));
        Assert.Equal(Sable.Palette.Accent, t.PixelAt(5, 15));
        Assert.Equal(Sable.Palette.Accent, t.PixelAt(5, 25));
    }

    [Fact]
    public void ColorScheme_picks_the_variant_of_the_current_family()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
                Swatch(ColorRole.Page),
                Swatch(ColorRole.Page).ColorScheme(ColorScheme.Dark))
            .Spacing(0)
            .Theme("Sable")), themes: Catalog());

        Assert.Equal(Sable.Palette.Page, t.PixelAt(5, 5));
        Assert.Equal(SableNight.Palette.Page, t.PixelAt(5, 15));
    }

    [Fact]
    public void An_exact_theme_keeps_its_variant_and_its_mode_is_inherited()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
                Swatch(ColorRole.Page),                          // Sable nuit, même si le système est clair
                Swatch(ColorRole.Page).Theme("Standard"))         // Standard… en sombre, comme son parent
            .Spacing(0)
            .Theme(SableNight)), themes: Catalog());

        Assert.Equal(SableNight.Palette.Page, t.PixelAt(5, 5));
        Assert.Equal(Theme.Dark.Palette.Page, t.PixelAt(5, 15));
    }

    [Fact]
    public void Unknown_theme_name_keeps_the_inherited_theme()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(Swatch(ColorRole.Page).Theme("absent"))), themes: Catalog());
        Assert.Equal(Theme.Light.Palette.Page, t.PixelAt(5, 5));
    }

    [Fact]
    public void Catalog_groups_themes_into_families()
    {
        var pair = new Themes(Sable, SableNight);   // sans nom : une famille nommée d'après le thème clair
        Assert.Single(pair.Families);
        Assert.Equal("Sable", pair.DefaultFamily.Name);
        Assert.True(pair.DefaultFamily.FollowsSystem);
        Assert.Same(SableNight, pair["sable NUIT"]);

        Assert.Equal("Standard", Themes.Default.DefaultFamily.Name);
        Assert.Equal("Test", ((Themes)Custom).DefaultFamily.Name);   // un seul thème : une famille à une variante
        Assert.Throws<ArgumentException>(() => new Themes { { "Deux clairs", Theme.Light, Sable } });
    }

    [Fact]
    public void Zoom_is_clamped()
    {
        using var t = new TestHost(new ViewProbe(() => Text("x")));
        t.Ui.Zoom = 10;
        Assert.Equal(3, t.Ui.Zoom);
        t.Ui.Zoom = 0.1f;
        Assert.Equal(0.5f, t.Ui.Zoom);
    }
}
