// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using MartletUI.Tests.Probes;
using MartletUI.Theming;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// Controls take their values from the resolved theme of their branch.
/// </summary>
public class ThemeTests
{
    private static readonly Theme s_light = Theme.Light;

    [AvaloniaFact]
    public void Text_uses_the_theme_font_and_color_unless_modified()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Text("corps"), Text("titre").Font(TextStyle.Title))));

        TextBlock body = t.TextShowing("corps");
        Assert.Equal(s_light.Typography.Body.Size, body.FontSize);
        Assert.Equal(s_light.Typography.Family, body.FontFamily.Name);
        Assert.Equal(s_light.Palette.Text.ToAvaloniaColor(), body.Foreground.ColorOf());

        TextBlock title = t.TextShowing("titre");
        Assert.Equal(s_light.Typography.Title.Size, title.FontSize);
        Assert.Equal((FontWeight)s_light.Typography.Title.Weight, title.FontWeight);
    }

    [AvaloniaFact]
    public void A_text_too_long_for_its_place_ends_with_an_ellipsis()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Text(new string('m', 200)))), width: 200);

        TextBlock text = t.Texts.Single();

        Assert.True(text.Bounds.Width <= 200);
        Assert.True(text.TextLayout.TextLines[0].HasCollapsed);
    }

    [AvaloniaFact]
    public void A_button_takes_the_colors_of_its_style()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
            HStack(Button("principal"), Button("secondaire").Style(Primitives.ButtonStyle.Secondary))));

        Button primary = t.ButtonLabeled("principal");
        Assert.Equal(s_light.Palette.Accent.ToAvaloniaColor(), primary.Background.ColorOf());
        Assert.Equal(s_light.Palette.OnAccent.ToAvaloniaColor(), primary.Foreground.ColorOf());
        Assert.Equal(new CornerRadius(s_light.Button.Radius), primary.CornerRadius);
        Assert.Equal(s_light.Palette.Secondary.ToAvaloniaColor(), t.ButtonLabeled("secondaire").Background.ColorOf());
    }

    [AvaloniaFact]
    public void A_branch_can_declare_its_own_color_scheme()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
            VStack(Text("clair"), VStack(Text("sombre")).ColorScheme(ColorScheme.Dark))));

        Assert.Equal(s_light.Palette.Text.ToAvaloniaColor(), t.TextShowing("clair").Foreground.ColorOf());
        Assert.Equal(Theme.Dark.Palette.Text.ToAvaloniaColor(), t.TextShowing("sombre").Foreground.ColorOf());
    }

    [AvaloniaFact]
    public void The_window_background_is_the_page_color_of_the_root_theme()
    {
        using var t = new MartletTestHost(new ViewProbe(() => Text("a").ColorScheme(ColorScheme.Dark)));

        Assert.Equal(Theme.Dark.Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());
    }

    // Colors, styles and dimensions added to the theme.

    private static readonly Theme s_custom = Theme.Light
        .Named("Test")
        .WithColor("success", "#00AA00")
        .WithTextStyle("caption", size: 10)
        .WithButtonStyle("success", new ButtonVariant(Background: "success", Foreground: ColorRole.OnAccent))
        .WithSpacing(20)
        with
    { DividerThickness = 3 };

    private static readonly Color s_success = Primitives.Color.Hex("#00AA00").ToAvaloniaColor();

    [AvaloniaFact]
    public void A_color_added_by_name_is_used_and_an_unknown_name_is_visible()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(
            Canvas(d => d.FillRect(d.Bounds, "success")).Size(20, 20),
            Canvas(d => d.FillRect(d.Bounds, "inconnue")).Size(20, 20)).Spacing(0)), themes: s_custom);

        Assert.Equal(s_success, t.PixelAt(10, 10));
        Assert.Equal(Palette.Missing.ToAvaloniaColor(), t.PixelAt(10, 30));
    }

    [AvaloniaFact]
    public void A_text_style_added_by_name_applies()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Text("petit").Font("caption"))), themes: s_custom);

        Assert.Equal(10, t.TextShowing("petit").FontSize);
    }

    [AvaloniaFact]
    public void A_button_style_added_by_name_applies()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Button("OK").Style("success"))), themes: s_custom);

        Assert.Equal(s_success, t.ButtonLabeled("OK").Background.ColorOf());
    }

    [AvaloniaFact]
    public void The_default_spacing_and_divider_thickness_come_from_the_theme()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Text("a"), Text("b"), Divider())), themes: s_custom);

        Assert.Equal(t.BoundsOf(t.TextShowing("a")).Bottom + 20, t.BoundsOf(t.TextShowing("b")).Y);
        Assert.Equal(3, t.Controls.OfType<Avalonia.Controls.Border>().Last(b => b.Parent is Panels.FlexStack).Bounds.Height);
    }
}
