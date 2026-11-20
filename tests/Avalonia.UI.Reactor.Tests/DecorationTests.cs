using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary><c>Divider()</c> et <c>Box()</c> : des vues faites de leurs seules valeurs de thème et modificateurs.</summary>
public class DecorationTests
{
    [AvaloniaFact]
    public void A_divider_is_a_full_width_line_with_the_thickness_and_color_of_the_theme()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text("a"), Divider(), Divider().Height(2).Background(ColorRole.Accent))));

        var lines = t.Controls.OfType<Border>().Where(b => b.Parent is FlexStack).ToList();
        Assert.Equal(new Size(400, Theme.Light.DividerThickness), lines[0].Bounds.Size);
        Assert.Equal(Theme.Light.Palette.Border.ToAvaloniaColor(), lines[0].Background.ColorOf());
        Assert.Equal(2, lines[1].Bounds.Height);
        Assert.Equal(Theme.Light.Palette.Accent.ToAvaloniaColor(), lines[1].Background.ColorOf());
    }

    [AvaloniaFact]
    public void A_box_is_a_surface_made_of_its_modifiers()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Box().Size(30, 20).Background(ColorRole.Accent).Radius(4))));

        var box = t.Controls.OfType<Surface>().Single();
        Assert.Equal(new Size(30, 20), box.Bounds.Size);
        Assert.Equal(new CornerRadius(4), box.CornerRadius);
        Assert.Equal(Theme.Light.Palette.Accent.ToAvaloniaColor(), box.Background.ColorOf());
    }
}
