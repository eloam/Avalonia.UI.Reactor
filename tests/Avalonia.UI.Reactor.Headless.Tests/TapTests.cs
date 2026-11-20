using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Headless.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Une vue cliquable (<c>.OnTap</c>) reçoit le clic sur toute sa surface et montre le voile du thème au survol.</summary>
public class TapTests
{
    [AvaloniaFact]
    public void A_tappable_view_runs_its_action_even_where_it_has_no_background()
    {
        int taps = 0;
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text("ligne").Padding(20).OnTap(() => taps++))));
        var surface = Assert.IsType<Surface>(t.TextShowing("ligne").Parent);

        t.ClickAt(t.BoundsOf(surface).TopLeft + new Vector(2, 2)); // dans la marge, hors du texte

        Assert.Equal(1, taps);
    }

    [AvaloniaFact]
    public void A_tappable_view_shows_the_hover_overlay_of_the_theme()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text("ligne").Padding(6).OnTap(() => { }))));
        var surface = Assert.IsType<Surface>(t.TextShowing("ligne").Parent);
        Assert.Equal((byte)0, surface.Background.ColorOf()?.A);

        t.Hover(surface);

        var light = Theme.Light;
        Assert.Equal(light.Overlay(Primitives.Color.Transparent, light.HoverOverlay).ToAvaloniaColor(), surface.Background.ColorOf());
    }
}
