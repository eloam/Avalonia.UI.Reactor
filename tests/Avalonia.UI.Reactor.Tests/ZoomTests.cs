using Avalonia.Headless.XUnit;
using Avalonia.Input;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Hosting;
using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Zoom de l'interface : borné, appliqué à la mise en page, piloté par Ctrl + / Ctrl − / Ctrl 0 (Cmd sur macOS).</summary>
public class ZoomTests
{
    [AvaloniaFact]
    public void Zoom_is_clamped()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => Text("x")));

        t.Host.Zoom = 10;
        Assert.Equal(ReactorHost.MaxZoom, t.Host.Zoom);
        t.Host.Zoom = 0.1;
        Assert.Equal(ReactorHost.MinZoom, t.Host.Zoom);
    }

    [AvaloniaFact]
    public void Zoom_scales_the_whole_interface()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Box().Size(100, 40).Background(ColorRole.Accent))));
        var accent = Theme.Light.Palette.Accent.ToAvaloniaColor();
        Assert.NotEqual(accent, t.PixelAt(150, 60));

        t.Host.Zoom = 2;
        t.Settle();

        Assert.Equal(accent, t.PixelAt(150, 60)); // la boîte de 100 × 40 en occupe maintenant 200 × 80
    }

    [AvaloniaFact]
    public void Zoom_shortcuts_step_by_ten_percent_within_bounds_and_reset()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => Text("x")));

        t.Press(Key.Add, PhysicalKey.NumPadAdd, ReactorTestHost.Command);
        t.Press(Key.OemPlus, PhysicalKey.Equal, ReactorTestHost.Command);
        Assert.Equal(1.2, t.Host.Zoom);

        for (int i = 0; i < 40; i++) t.Press(Key.OemMinus, PhysicalKey.Minus, ReactorTestHost.Command);
        Assert.Equal(ReactorHost.MinZoom, t.Host.Zoom);

        t.Press(Key.D0, PhysicalKey.Digit0, ReactorTestHost.Command);
        Assert.Equal(1, t.Host.Zoom);
    }

    [AvaloniaFact]
    public void Zoom_shortcuts_can_be_disabled()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => Text("x")));
        t.Host.ZoomShortcuts = false;

        t.Press(Key.OemPlus, PhysicalKey.Equal, ReactorTestHost.Command);

        Assert.Equal(1, t.Host.Zoom);
    }
}
