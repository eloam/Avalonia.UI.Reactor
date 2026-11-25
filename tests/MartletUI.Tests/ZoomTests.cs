// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using MartletUI.Hosting;
using MartletUI.Tests.Probes;
using MartletUI.Theming;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// Interface zoom: bounded, applied to the layout, driven by Ctrl + / Ctrl − / Ctrl 0 (Cmd on macOS).
/// </summary>
public class ZoomTests
{
    [AvaloniaFact]
    public void Zoom_is_clamped()
    {
        using var t = new MartletTestHost(new ViewProbe(() => Text("x")));

        t.Host.Zoom = 10;
        Assert.Equal(MartletHost.MaxZoom, t.Host.Zoom);
        t.Host.Zoom = 0.1;
        Assert.Equal(MartletHost.MinZoom, t.Host.Zoom);
    }

    [AvaloniaFact]
    public void Zoom_scales_the_whole_interface()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Box().Size(100, 40).Background(ColorRole.Accent))));
        Color accent = Theme.Light.Palette.Accent.ToAvaloniaColor();
        Assert.NotEqual(accent, t.PixelAt(150, 60));

        t.Host.Zoom = 2;
        t.Settle();

        // The 100 × 40 box now covers 200 × 80.
        Assert.Equal(accent, t.PixelAt(150, 60));
    }

    [AvaloniaFact]
    public void Zoom_shortcuts_step_by_ten_percent_within_bounds_and_reset()
    {
        using var t = new MartletTestHost(new ViewProbe(() => Text("x")));

        t.Press(Key.Add, PhysicalKey.NumPadAdd, MartletTestHost.Command);
        t.Press(Key.OemPlus, PhysicalKey.Equal, MartletTestHost.Command);
        Assert.Equal(1.2, t.Host.Zoom);

        for (int i = 0; i < 40; i++)
            t.Press(Key.OemMinus, PhysicalKey.Minus, MartletTestHost.Command);
        Assert.Equal(MartletHost.MinZoom, t.Host.Zoom);

        t.Press(Key.D0, PhysicalKey.Digit0, MartletTestHost.Command);
        Assert.Equal(1, t.Host.Zoom);
    }

    [AvaloniaFact]
    public void Zoom_shortcuts_can_be_disabled()
    {
        using var t = new MartletTestHost(new ViewProbe(() => Text("x")));
        t.Host.ZoomShortcuts = false;

        t.Press(Key.OemPlus, PhysicalKey.Equal, MartletTestHost.Command);

        Assert.Equal(1, t.Host.Zoom);
    }
}
