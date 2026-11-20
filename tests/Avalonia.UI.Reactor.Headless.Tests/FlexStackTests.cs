using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Headless.Tests.Probes;
using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Les piles suivent les règles de SwiftUI, comme dans l'ancien moteur (fenêtre de 400 × 300).</summary>
public class FlexStackTests
{
    [AvaloniaFact]
    public void A_spacer_pushes_the_following_views_to_the_end()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => HStack(Text("a"), Spacer(), Button("b"))));

        Assert.Equal(0, t.BoundsOf(t.TextShowing("a")).X);
        Assert.Equal(400, t.BoundsOf(t.ButtonLabeled("b")).Right);
    }

    [AvaloniaFact]
    public void Grow_shares_the_remaining_space_in_proportion()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => HStack(Spacer().Grow(1), Spacer().Grow(3)).Spacing(0)));

        var spacers = t.Controls.OfType<FlexStack>().Single().Children;
        Assert.Equal(100, spacers[0].Bounds.Width);
        Assert.Equal(300, spacers[1].Bounds.Width);
    }

    [AvaloniaFact]
    public void A_vertical_stack_aligns_its_children_to_the_start_and_spaces_them_by_the_theme()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text("court"), Text("un texte plus long"))));

        var first = t.BoundsOf(t.TextShowing("court"));
        var second = t.BoundsOf(t.TextShowing("un texte plus long"));
        Assert.Equal(0, first.X);
        Assert.Equal(0, second.X);
        Assert.True(first.Width < second.Width);
        Assert.Equal(first.Bottom + Theme.Light.Spacing, second.Y);
    }

    [AvaloniaFact]
    public void A_horizontal_stack_centers_its_children_vertically()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => HStack(Text("a"), Button("b"))));

        Assert.Equal(t.BoundsOf(t.ButtonLabeled("b")).Center.Y, t.BoundsOf(t.TextShowing("a")).Center.Y, tolerance: 1);
    }

    [AvaloniaFact]
    public void FillWidth_stretches_a_child_across_the_stack()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text("a").FillWidth(), Text("b"))));

        Assert.Equal(400, t.TextShowing("a").Bounds.Width);
        Assert.True(t.TextShowing("b").Bounds.Width < 400);
    }

    [AvaloniaFact]
    public void A_stack_with_a_spacer_stretches_without_being_told()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(HStack(Text("a"), Spacer(), Text("b")), Text("c"))));

        Assert.Equal(400, t.BoundsOf(t.TextShowing("b")).Right);
    }

    [AvaloniaFact]
    public void Padding_background_border_and_radius_are_drawn_by_an_enclosing_surface()
    {
        using var t = new ReactorTestHost(new ViewProbe(() =>
            VStack(Text("a")).Padding(16).Background(ColorRole.Surface).Border(ColorRole.Border, 2).Radius(10)));

        var surface = Assert.IsType<Surface>(t.Host.Content);
        Assert.Equal(new Thickness(16), surface.Padding);
        Assert.Equal(new Thickness(2), surface.BorderThickness);
        Assert.Equal(new CornerRadius(10), surface.CornerRadius);
        Assert.Equal(Theme.Light.Palette.Surface.ToAvaloniaColor(), surface.Background.ColorOf());
        // La bordure prend sa place, comme en CSS et dans les contrôles Avalonia : le contenu commence après elle.
        Assert.Equal(new Point(18, 18), t.BoundsOf(t.TextShowing("a")).TopLeft);
    }
}
