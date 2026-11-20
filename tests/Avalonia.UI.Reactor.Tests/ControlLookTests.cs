using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// Nos contrôles portent l'apparence du thème de leur branche, et non celle de Fluent : pixels rendus dans les quatre
/// variantes, puis couleurs de chaque état (survol, appui, focus).
/// </summary>
public class ControlLookTests
{
    static ReactorTestHost Show(string family, ColorScheme scheme) => new(new ViewProbe(() =>
            VStack(Button("Bouton"), Checkbox(true, onChange: null, label: "Case"), TextField("texte", onChange: null))
                .Padding(16).Theme(family).ColorScheme(scheme)),
        themes: TestThemes.Create());

    [AvaloniaTheory]
    [InlineData("Standard", ColorScheme.Light)]
    [InlineData("Standard", ColorScheme.Dark)]
    [InlineData("Sable", ColorScheme.Light)]
    [InlineData("Sable", ColorScheme.Dark)]
    public void Controls_are_painted_with_the_colors_of_their_theme(string family, ColorScheme scheme)
    {
        var theme = TestThemes.Of(family, scheme);
        using var t = Show(family, scheme);

        var button = t.BoundsOf(t.ButtonLabeled("Bouton"));
        var box = t.BoundsOf(t.Controls.OfType<CheckGlyph>().Single());
        var field = t.BoundsOf(t.Field);

        Assert.Equal(theme.Palette.Page.ToAvaloniaColor(), t.PixelAt(4, 4));
        Assert.Equal(theme.Palette.Accent.ToAvaloniaColor(), t.PixelAt(button.X + 6, button.Center.Y));
        Assert.Equal(theme[theme.Checkbox.CheckedBackground].ToAvaloniaColor(), t.PixelAt(box.X + 4, box.Y + 4));
        Assert.Equal(theme[theme.TextField.Background].ToAvaloniaColor(), t.PixelAt(field.X + 4, field.Center.Y));
        Assert.Equal(theme[theme.TextField.BorderColor].ToAvaloniaColor(), t.PixelAt(field.Center.X, field.Y));
    }

    [AvaloniaFact]
    public void A_button_takes_the_hover_then_the_pressed_overlay_of_its_variant()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Button("Bouton"))));
        var light = Theme.Light;
        var accent = light.Palette.Accent;
        var button = t.ButtonLabeled("Bouton");

        t.Hover(button);
        Assert.Equal(light.Overlay(accent, light.Button.Primary.HoverOverlay).ToAvaloniaColor(), button.Background.ColorOf());

        t.Window.MouseDown(t.BoundsOf(button).Center, MouseButton.Left);
        t.Settle();
        Assert.Equal(light.Overlay(accent, light.Button.Primary.PressedOverlay).ToAvaloniaColor(), button.Background.ColorOf());
    }

    [AvaloniaFact]
    public void A_focused_field_takes_the_focus_border_without_moving_its_text()
    {
        using var t = new ReactorTestHost(new FormProbe());
        var style = Theme.Light.TextField;
        var text = t.Controls.OfType<TextPresenter>().Single();
        var before = t.BoundsOf(text).TopLeft;

        t.Click(t.Field);

        Assert.Equal(Theme.Light[style.FocusBorderColor].ToAvaloniaColor(), t.Field.BorderBrush.ColorOf());
        Assert.Equal(new Thickness(style.FocusBorderWidth), t.Field.BorderThickness);
        Assert.Equal(before, t.BoundsOf(text).TopLeft);
    }

    [AvaloniaFact]
    public void The_window_follows_the_color_scheme_of_the_system()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(Text("a"))));

        t.Host.Themes.SystemScheme = ColorScheme.Dark;
        t.Settle();

        Assert.Equal(Theme.Dark.Palette.Page.ToAvaloniaColor(), t.PixelAt(200, 200));
    }
}
