// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Control templates (<c>.Template(…)</c> or the template of the theme): the control keeps its behavior, its look is
/// the view of the template, recomputed when its state changes; the editing area of a field sits on <c>field.Editor</c>.
/// </summary>
public class TemplateTests
{
    private static ReactorTestHost Show(TemplateProbe probe) => new(probe, themes: TemplateProbe.Look);

    /// <summary>
    /// Returns a text displayed anywhere, including inside a control, where the view of a template lives.
    /// </summary>
    private static TextBlock? Shown(ReactorTestHost t, string text) => t.Controls.OfType<TextBlock>().SingleOrDefault(b => b.Text == text);

    [AvaloniaFact]
    public void A_button_shows_its_template_and_keeps_its_click()
    {
        var probe = new TemplateProbe();
        using ReactorTestHost t = Show(probe);
        ReactorButton button = t.Controls.OfType<ReactorButton>().Single();
        TextBlock? look = Shown(t, "[OK 0]");
        Assert.NotNull(look);

        t.Click(button);

        Assert.Equal(1, probe.Clicks);
        // Same control, new value: the pointer stayed over it.
        Assert.Same(look, Shown(t, "<OK>"));
    }

    [AvaloniaFact]
    public void A_template_is_recomputed_when_the_control_state_changes()
    {
        using ReactorTestHost t = Show(new TemplateProbe());

        t.Hover(t.Controls.OfType<ReactorButton>().Single());

        Assert.NotNull(Shown(t, "<OK>"));
    }

    [AvaloniaFact]
    public void A_checkbox_template_replaces_the_drawn_box_and_follows_the_state()
    {
        var probe = new TemplateProbe();
        using ReactorTestHost t = Show(probe);
        ReactorCheckBox box = t.Controls.OfType<ReactorCheckBox>().Single();
        Assert.False(t.Controls.OfType<CheckGlyph>().Single().IsVisible);

        t.Click(box);

        Assert.True(probe.Done);
        Assert.NotNull(Shown(t, "[x] fait"));
    }

    [AvaloniaFact]
    public void The_editing_area_of_a_templated_field_sits_on_its_editor_slot()
    {
        var probe = new TemplateProbe();
        using ReactorTestHost t = Show(probe);
        EditorSlot slot = t.Controls.OfType<EditorSlot>().Single();
        TextPresenter text = t.Controls.OfType<TextPresenter>().Single();

        Assert.Equal(t.BoundsOf(slot).TopLeft, t.BoundsOf(text.FindAncestorOfType<ScrollViewer>()!).TopLeft);

        t.Click(t.Field);
        t.Type("souligné");
        Assert.Equal("souligné", probe.Draft);
        Border line = t.Controls.OfType<Border>().Single(b => b.Parent is FlexStack && b.FindAncestorOfType<TextBox>() is not null);
        // The template sees the focus.
        Assert.Equal(TemplateProbe.Look.Palette.Accent.ToAvaloniaColor(), line.Background.ColorOf());
    }

    [AvaloniaFact]
    public void A_template_defines_the_size_of_its_control()
    {
        using var t = new ReactorTestHost(new ViewProbe(() =>
            Views.Ui.VStack(Views.Ui.Button("OK").Template(_ => Views.Ui.Box().Size(120, 44)))));

        Assert.Equal(new Size(120, 44), t.Controls.OfType<ReactorButton>().Single().Bounds.Size);
    }

    [AvaloniaFact]
    public void A_template_on_the_view_wins_over_the_theme()
    {
        Theme look = TemplateProbe.Look.WithButton(b => b with { Template = s => Views.Ui.Text($"thème {s.Label}") });
        using var t = new ReactorTestHost(new ViewProbe(() => Views.Ui.VStack(
            Views.Ui.Button("A"),
            Views.Ui.Button("B").Template(s => Views.Ui.Text($"vue {s.Label}")))), themes: look);

        Assert.NotNull(Shown(t, "thème A"));
        Assert.NotNull(Shown(t, "vue B"));
        Assert.Null(Shown(t, "thème B"));
    }
}
