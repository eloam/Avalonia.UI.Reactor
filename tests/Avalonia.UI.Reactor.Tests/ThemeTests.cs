using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Les contrôles prennent leurs valeurs dans le thème résolu de leur branche.</summary>
public class ThemeTests
{
    static readonly Theme Light = Theme.Light;

    [AvaloniaFact]
    public void Text_uses_the_theme_font_and_color_unless_modified()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text("corps"), Text("titre").Font(TextStyle.Title))));

        var body = t.TextShowing("corps");
        Assert.Equal(Light.Typography.Body.Size, body.FontSize);
        Assert.Equal(Light.Typography.Family, body.FontFamily.Name);
        Assert.Equal(Light.Palette.Text.ToAvaloniaColor(), body.Foreground.ColorOf());

        var title = t.TextShowing("titre");
        Assert.Equal(Light.Typography.Title.Size, title.FontSize);
        Assert.Equal((FontWeight)Light.Typography.Title.Weight, title.FontWeight);
    }

    [AvaloniaFact]
    public void A_text_too_long_for_its_place_ends_with_an_ellipsis()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text(new string('m', 200)))), width: 200);

        var text = t.Texts.Single();

        Assert.True(text.Bounds.Width <= 200);
        Assert.True(text.TextLayout.TextLines[0].HasCollapsed);
    }

    [AvaloniaFact]
    public void A_button_takes_the_colors_of_its_style()
    {
        using var t = new ReactorTestHost(new ViewProbe(() =>
            HStack(Button("principal"), Button("secondaire").Style(Primitives.ButtonStyle.Secondary))));

        var primary = t.ButtonLabeled("principal");
        Assert.Equal(Light.Palette.Accent.ToAvaloniaColor(), primary.Background.ColorOf());
        Assert.Equal(Light.Palette.OnAccent.ToAvaloniaColor(), primary.Foreground.ColorOf());
        Assert.Equal(new CornerRadius(Light.Button.Radius), primary.CornerRadius);
        Assert.Equal(Light.Palette.Secondary.ToAvaloniaColor(), t.ButtonLabeled("secondaire").Background.ColorOf());
    }

    [AvaloniaFact]
    public void A_branch_can_declare_its_own_color_scheme()
    {
        using var t = new ReactorTestHost(new ViewProbe(() =>
            VStack(Text("clair"), VStack(Text("sombre")).ColorScheme(ColorScheme.Dark))));

        Assert.Equal(Light.Palette.Text.ToAvaloniaColor(), t.TextShowing("clair").Foreground.ColorOf());
        Assert.Equal(Theme.Dark.Palette.Text.ToAvaloniaColor(), t.TextShowing("sombre").Foreground.ColorOf());
    }

    [AvaloniaFact]
    public void The_window_background_is_the_page_color_of_the_root_theme()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => Text("a").ColorScheme(ColorScheme.Dark)));

        Assert.Equal(Theme.Dark.Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());
    }

    // ── Couleurs, styles et dimensions ajoutés au thème ──

    static readonly Theme Custom = Theme.Light
        .Named("Test")
        .WithColor("success", "#00AA00")
        .WithTextStyle("caption", size: 10)
        .WithButtonStyle("success", new ButtonVariant(Background: "success", Foreground: ColorRole.OnAccent))
        .WithSpacing(20)
        with { DividerThickness = 3 };

    static readonly Color Success = Primitives.Color.Hex("#00AA00").ToAvaloniaColor();

    [AvaloniaFact]
    public void A_color_added_by_name_is_used_and_an_unknown_name_is_visible()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(
            Canvas(d => d.FillRect(d.Bounds, "success")).Size(20, 20),
            Canvas(d => d.FillRect(d.Bounds, "inconnue")).Size(20, 20)).Spacing(0)), themes: Custom);

        Assert.Equal(Success, t.PixelAt(10, 10));
        Assert.Equal(Palette.Missing.ToAvaloniaColor(), t.PixelAt(10, 30));
    }

    [AvaloniaFact]
    public void A_text_style_added_by_name_applies()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text("petit").Font("caption"))), themes: Custom);

        Assert.Equal(10, t.TextShowing("petit").FontSize);
    }

    [AvaloniaFact]
    public void A_button_style_added_by_name_applies()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Button("OK").Style("success"))), themes: Custom);

        Assert.Equal(Success, t.ButtonLabeled("OK").Background.ColorOf());
    }

    [AvaloniaFact]
    public void The_default_spacing_and_divider_thickness_come_from_the_theme()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text("a"), Text("b"), Divider())), themes: Custom);

        Assert.Equal(t.BoundsOf(t.TextShowing("a")).Bottom + 20, t.BoundsOf(t.TextShowing("b")).Y);
        Assert.Equal(3, t.Controls.OfType<Avalonia.Controls.Border>().Last(b => b.Parent is Panels.FlexStack).Bounds.Height);
    }
}
