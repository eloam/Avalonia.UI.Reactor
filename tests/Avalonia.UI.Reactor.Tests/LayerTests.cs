using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary><c>ZStack</c>, <c>Overlay</c> et modificateurs d'un composant : des couches superposées.</summary>
public class LayerTests
{
    [AvaloniaFact]
    public void An_overlay_is_placed_by_its_alignment_over_the_view()
    {
        using var t = new ReactorTestHost(new ViewProbe(() =>
            VStack(Box().Size(100, 40).Background(ColorRole.Surface).Overlay(Text("x"), Primitives.Alignment.TopRight))));

        var box = t.BoundsOf(t.Controls.OfType<Surface>().Single());
        var badge = t.BoundsOf(t.TextShowing("x"));
        Assert.Equal(box.Right, badge.Right);
        Assert.Equal(box.Top, badge.Top);
    }

    [AvaloniaFact]
    public void Layers_take_the_size_of_the_largest_one()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(ZStack(Box().Size(60, 20), Box().Size(30, 50)))));

        Assert.Equal(new Size(60, 50), t.Controls.OfType<LayerStack>().Single().Bounds.Size);
    }

    [AvaloniaFact]
    public void A_component_with_modifiers_keeps_its_state_and_its_own_updates()
    {
        var probe = new PaddedCounterProbe();
        using var t = new ReactorTestHost(probe);
        var text = t.TextShowing("0");

        t.ClickButton("+");

        Assert.Same(text, t.TextShowing("1"));
        Assert.Equal(new UpdateStats(Evaluated: 1, Reused: 0, Scopes: 2), t.Stats);
        Assert.Equal(8, t.BoundsOf(t.ButtonLabeled("−")).X);
    }

    [AvaloniaFact]
    public void The_topmost_layer_receives_the_click()
    {
        var clicked = "";
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(ZStack(
            Button("dessous", () => clicked = "dessous").Size(100, 40),
            Button("dessus", () => clicked = "dessus").Size(100, 40)))));

        t.Click(t.ButtonLabeled("dessous")); // même zone : c'est le bouton du dessus qui reçoit le clic

        Assert.Equal("dessus", clicked);
    }
}
