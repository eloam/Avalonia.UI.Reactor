// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using MartletUI.Panels;
using MartletUI.State;
using MartletUI.Tests.Probes;
using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// <c>ZStack</c>, <c>Overlay</c> and the modifiers of a component: layered views.
/// </summary>
public class LayerTests
{
    [AvaloniaFact]
    public void An_overlay_is_placed_by_its_alignment_over_the_view()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
            VStack(Box().Size(100, 40).Background(ColorRole.Surface).Overlay(Text("x"), Primitives.Alignment.TopRight))));

        Rect box = t.BoundsOf(t.Controls.OfType<Surface>().Single());
        Rect badge = t.BoundsOf(t.TextShowing("x"));
        Assert.Equal(box.Right, badge.Right);
        Assert.Equal(box.Top, badge.Top);
    }

    [AvaloniaFact]
    public void Layers_take_the_size_of_the_largest_one()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(ZStack(Box().Size(60, 20), Box().Size(30, 50)))));

        Assert.Equal(new Size(60, 50), t.Controls.OfType<LayerStack>().Single().Bounds.Size);
    }

    [AvaloniaFact]
    public void A_component_with_modifiers_keeps_its_state_and_its_own_updates()
    {
        var probe = new PaddedCounterProbe();
        using var t = new MartletTestHost(probe);
        TextBlock text = t.TextShowing("0");

        t.ClickButton("+");

        Assert.Same(text, t.TextShowing("1"));
        Assert.Equal(new UpdateStats(Evaluated: 1, Reused: 0, Scopes: 2), t.Stats);
        Assert.Equal(8, t.BoundsOf(t.ButtonLabeled("−")).X);
    }

    [AvaloniaFact]
    public void The_topmost_layer_receives_the_click()
    {
        string clicked = "";
        using var t = new MartletTestHost(new ViewProbe(() => VStack(ZStack(
            Button("dessous", () => clicked = "dessous").Size(100, 40),
            Button("dessus", () => clicked = "dessus").Size(100, 40)))));

        // Same area: the button on top receives the click.
        t.Click(t.ButtonLabeled("dessous"));

        Assert.Equal("dessus", clicked);
    }
}
