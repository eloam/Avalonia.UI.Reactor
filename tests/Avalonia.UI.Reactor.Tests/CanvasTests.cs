// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// <c>Canvas</c> really draws (pixels read after a Skia rendering), in the colors of the theme, within its bounds.
/// </summary>
public class CanvasTests
{
    [AvaloniaFact]
    public void A_canvas_draws_with_the_colors_of_the_theme()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Canvas(d => d.FillRect(d.Bounds, ColorRole.Accent)).Size(40, 20))));

        Assert.Equal(Theme.Light.Palette.Accent.ToAvaloniaColor(), t.PixelAt(20, 10));
    }

    [AvaloniaFact]
    public void A_canvas_does_not_draw_outside_its_bounds()
    {
        using var t = new ReactorTestHost(new ViewProbe(() =>
            VStack(Canvas(d => d.FillRect(new Primitives.Rect(-50, -50, 500, 500), ColorRole.Accent)).Size(40, 20))));

        Assert.Equal(Theme.Light.Palette.Page.ToAvaloniaColor(), t.PixelAt(60, 10));
        Assert.Equal(Theme.Light.Palette.Page.ToAvaloniaColor(), t.PixelAt(20, 30));
    }

    [AvaloniaFact]
    public void A_canvas_is_redrawn_when_a_state_read_while_drawing_changes()
    {
        var probe = new SwatchProbe();
        using var t = new ReactorTestHost(probe);
        Assert.Equal(Colors.Blue, t.PixelAt(20, 10));

        t.ClickButton("Rouge");

        Assert.Equal(Colors.Red, t.PixelAt(20, 10));
    }
}
