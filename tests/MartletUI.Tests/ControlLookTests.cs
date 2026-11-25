// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using MartletUI.Tests.Probes;
using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// Our controls carry the look of the theme of their branch, not that of Fluent: pixels rendered in the four
/// variants, then the colors of each state (hover, pressed, focus).
/// </summary>
public class ControlLookTests
{
    private static MartletTestHost Show(string family, ColorScheme scheme) => new(new ViewProbe(() =>
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
        Theme theme = TestThemes.Of(family, scheme);
        using MartletTestHost t = Show(family, scheme);

        Rect button = t.BoundsOf(t.ButtonLabeled("Bouton"));
        Rect box = t.BoundsOf(t.Controls.OfType<CheckGlyph>().Single());
        Rect field = t.BoundsOf(t.Field);

        Assert.Equal(theme.Palette.Page.ToAvaloniaColor(), t.PixelAt(4, 4));
        Assert.Equal(theme.Palette.Accent.ToAvaloniaColor(), t.PixelAt(button.X + 6, button.Center.Y));
        Assert.Equal(theme[theme.Checkbox.CheckedBackground].ToAvaloniaColor(), t.PixelAt(box.X + 4, box.Y + 4));
        Assert.Equal(theme[theme.TextField.Background].ToAvaloniaColor(), t.PixelAt(field.X + 4, field.Center.Y));
        Assert.Equal(theme[theme.TextField.BorderColor].ToAvaloniaColor(), t.PixelAt(field.Center.X, field.Y));
    }

    [AvaloniaFact]
    public void A_button_takes_the_hover_then_the_pressed_overlay_of_its_variant()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Button("Bouton"))));
        Theme light = Theme.Light;
        Primitives.Color accent = light.Palette.Accent;
        Button button = t.ButtonLabeled("Bouton");

        t.Hover(button);
        Assert.Equal(light.Overlay(accent, light.Button.Primary.HoverOverlay).ToAvaloniaColor(), button.Background.ColorOf());

        t.Window.MouseDown(t.BoundsOf(button).Center, MouseButton.Left);
        t.Settle();
        Assert.Equal(light.Overlay(accent, light.Button.Primary.PressedOverlay).ToAvaloniaColor(), button.Background.ColorOf());
    }

    [AvaloniaFact]
    public void A_focused_field_takes_the_focus_border_without_moving_its_text()
    {
        using var t = new MartletTestHost(new FormProbe());
        TextFieldTheme style = Theme.Light.TextField;
        TextPresenter text = t.Controls.OfType<TextPresenter>().Single();
        Point before = t.BoundsOf(text).TopLeft;

        t.Click(t.Field);

        Assert.Equal(Theme.Light[style.FocusBorderColor].ToAvaloniaColor(), t.Field.BorderBrush.ColorOf());
        Assert.Equal(new Thickness(style.FocusBorderWidth), t.Field.BorderThickness);
        Assert.Equal(before, t.BoundsOf(text).TopLeft);
    }

    [AvaloniaFact]
    public void The_window_follows_the_color_scheme_of_the_system()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Text("a"))));

        t.Host.Themes.SystemScheme = ColorScheme.Dark;
        t.Settle();

        Assert.Equal(Theme.Dark.Palette.Page.ToAvaloniaColor(), t.PixelAt(200, 200));
    }
}
