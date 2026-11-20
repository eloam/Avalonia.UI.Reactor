using Mvu.Primitives;
using Mvu.Theming;
using Mvu.Views;

namespace Mvu.Tests;

/// <summary>ZStack, Overlay, Canvas et modificateurs sur les composants.</summary>
public class ExtensibilityTests
{
    [Fact]
    public void ZStack_sizes_to_largest_child_and_centers_others()
    {
        using var t = new TestHost(new ViewProbe(() =>
            VStack(ZStack(Box().Size(100, 50).Key("fond"), Box().Size(20, 10).Key("centre")))));

        Assert.Equal(new Rect(0, 0, 100, 50), t.BoundsOfKey("fond"));
        Assert.Equal(new Rect(40, 20, 20, 10), t.BoundsOfKey("centre"));
    }

    [Fact]
    public void Overlay_places_a_badge_in_the_corner()
    {
        using var t = new TestHost(new ViewProbe(() =>
            VStack(Box().Size(100, 50).Overlay(Box().Size(10, 10).Key("badge"), Alignment.TopRight))));

        Assert.Equal(new Rect(90, 0, 10, 10), t.BoundsOfKey("badge"));
    }

    [Fact]
    public void Topmost_child_of_a_ZStack_receives_the_click()
    {
        var clicked = "";
        using var t = new TestHost(new ViewProbe(() =>
            VStack(ZStack(
                Button("dessous", () => clicked = "dessous").Size(100, 40),
                Button("dessus", () => clicked = "dessus").Size(100, 40)))));

        t.Click(v => v is ButtonView { Label: "dessous" }); // même zone : c'est celui du dessus qui reçoit
        Assert.Equal("dessus", clicked);
    }

    [Fact]
    public void Modifiers_apply_around_a_component()
    {
        var child = new TextProbe("enfant");
        using var t = new TestHost(new ViewProbe(() => VStack(child.Padding(10).Background(ColorRole.Surface))));

        var text = t.BoundsOf(v => v is TextView { Content: "enfant" });
        Assert.Equal(10, text.X);
        Assert.Equal(10, text.Y);
    }

    [Fact]
    public void Wrapped_component_still_fills_and_keeps_its_own_updates()
    {
        var child = new FillProbe();
        using var t = new TestHost(new ViewProbe(() => VStack(child.Padding(8))), width: 300);

        Assert.Equal(300 - 16, t.BoundsOf(v => v is ButtonView).Width); // FillWidth du contenu traverse l'enveloppe

        t.ClickButton("0");
        Assert.True(t.HasButton("1"));
        Assert.Equal(1, t.Stats.Evaluated); // seul le composant enfant est recalculé
    }

    [Fact]
    public void Canvas_draws_with_fixed_colors_and_theme_roles()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            Canvas(d => d.FillRect(d.Bounds, "#FF0000")).Size(20, 20),
            Canvas(d => d.FillCircle(d.Bounds.Center, 10, ColorRole.Accent)).Size(20, 20))
            .Spacing(0)));

        Assert.Equal(new Color(255, 0, 0), t.PixelAt(10, 10));
        Assert.Equal(Theme.Light.Palette.Accent, t.PixelAt(10, 30));
    }

    [Fact]
    public void ProgressRing_from_the_demo_shows_percentage()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(Widgets.ProgressRing(1, 4))));
        Assert.True(t.HasText("25%"));
    }
}
