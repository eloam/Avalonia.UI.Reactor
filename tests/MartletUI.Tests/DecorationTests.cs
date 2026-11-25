// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using MartletUI.Panels;
using MartletUI.Tests.Probes;
using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// <c>Divider()</c> and <c>Box()</c>: views made of their theme values and modifiers only.
/// </summary>
public class DecorationTests
{
    [AvaloniaFact]
    public void A_divider_is_a_full_width_line_with_the_thickness_and_color_of_the_theme()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Text("a"), Divider(), Divider().Height(2).Background(ColorRole.Accent))));

        var lines = t.Controls.OfType<Border>().Where(b => b.Parent is FlexStack).ToList();
        Assert.Equal(new Size(400, Theme.Light.DividerThickness), lines[0].Bounds.Size);
        Assert.Equal(Theme.Light.Palette.Border.ToAvaloniaColor(), lines[0].Background.ColorOf());
        Assert.Equal(2, lines[1].Bounds.Height);
        Assert.Equal(Theme.Light.Palette.Accent.ToAvaloniaColor(), lines[1].Background.ColorOf());
    }

    [AvaloniaFact]
    public void A_box_is_a_surface_made_of_its_modifiers()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Box().Size(30, 20).Background(ColorRole.Accent).Radius(4))));

        Surface box = t.Controls.OfType<Surface>().Single();
        Assert.Equal(new Size(30, 20), box.Bounds.Size);
        Assert.Equal(new CornerRadius(4), box.CornerRadius);
        Assert.Equal(Theme.Light.Palette.Accent.ToAvaloniaColor(), box.Background.ColorOf());
    }
}
