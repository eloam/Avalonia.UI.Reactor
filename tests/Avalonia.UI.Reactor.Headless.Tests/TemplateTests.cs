using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Headless.Tests.Probes;
using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Modèles de contrôles (<c>.Template(…)</c> ou modèle du thème) : le contrôle garde son comportement, son apparence est
/// la vue du modèle, recalculée quand son état change ; la zone d'édition d'un champ se pose sur <c>field.Editor</c>.
/// </summary>
public class TemplateTests
{
    static ReactorTestHost Show(TemplateProbe probe) => new(probe, themes: TemplateProbe.Look);

    /// <summary>Texte affiché n'importe où, y compris dans un contrôle (la vue d'un modèle y vit).</summary>
    static TextBlock? Shown(ReactorTestHost t, string text) => t.Controls.OfType<TextBlock>().SingleOrDefault(b => b.Text == text);

    [AvaloniaFact]
    public void A_button_shows_its_template_and_keeps_its_click()
    {
        var probe = new TemplateProbe();
        using var t = Show(probe);
        var button = t.Controls.OfType<ReactorButton>().Single();
        var look = Shown(t, "[OK 0]");
        Assert.NotNull(look);

        t.Click(button);

        Assert.Equal(1, probe.Clicks);
        Assert.Same(look, Shown(t, "<OK>")); // même contrôle, nouvelle valeur (le pointeur est resté dessus)
    }

    [AvaloniaFact]
    public void A_template_is_recomputed_when_the_control_state_changes()
    {
        using var t = Show(new TemplateProbe());

        t.Hover(t.Controls.OfType<ReactorButton>().Single());

        Assert.NotNull(Shown(t, "<OK>"));
    }

    [AvaloniaFact]
    public void A_checkbox_template_replaces_the_drawn_box_and_follows_the_state()
    {
        var probe = new TemplateProbe();
        using var t = Show(probe);
        var box = t.Controls.OfType<ReactorCheckBox>().Single();
        Assert.False(t.Controls.OfType<CheckGlyph>().Single().IsVisible);

        t.Click(box);

        Assert.True(probe.Done);
        Assert.NotNull(Shown(t, "[x] fait"));
    }

    [AvaloniaFact]
    public void The_editing_area_of_a_templated_field_sits_on_its_editor_slot()
    {
        var probe = new TemplateProbe();
        using var t = Show(probe);
        var slot = t.Controls.OfType<EditorSlot>().Single();
        var text = t.Controls.OfType<TextPresenter>().Single();

        Assert.Equal(t.BoundsOf(slot).TopLeft, t.BoundsOf(text.FindAncestorOfType<ScrollViewer>()!).TopLeft);

        t.Click(t.Field);
        t.Type("souligné");
        Assert.Equal("souligné", probe.Draft);
        var line = t.Controls.OfType<Border>().Single(b => b.Parent is FlexStack && b.FindAncestorOfType<TextBox>() is not null);
        Assert.Equal(TemplateProbe.Look.Palette.Accent.ToAvaloniaColor(), line.Background.ColorOf()); // le modèle voit le focus
    }
}
