using Mvu.Primitives;
using Mvu.Runtime;
using Mvu.Tests.Probes;

namespace Mvu.Tests;

/// <summary>Raccourcis gérés par le runtime, quel que soit l'hôte : coller et zoom.</summary>
public class EditingShortcutTests
{
    [Fact]
    public void Ctrl_V_pastes_the_clipboard_text_on_one_line_at_the_caret()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);
        t.Ui.Clipboard = new FakeClipboard("deux\r\nlignes");
        t.Click(v => v.Key == "field");
        t.Type("[]");
        t.Press(KeyCode.Left);

        Assert.True(t.Press(KeyCode.V, KeyModifiers.Ctrl));
        t.Ui.TextInput("v"); // caractère éventuellement envoyé par la plateforme avec la touche

        Assert.Equal("[deux lignes]", probe.Draft);
    }

    [Fact]
    public void Ctrl_V_without_a_focused_field_or_clipboard_text_does_nothing()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);
        t.Ui.Clipboard = new FakeClipboard();
        t.Press(KeyCode.V, KeyModifiers.Ctrl);

        t.Click(v => v.Key == "field");
        t.Press(KeyCode.V, KeyModifiers.Ctrl);

        Assert.Equal("", probe.Draft);
    }

    [Fact]
    public void Zoom_shortcuts_step_by_ten_percent_within_bounds_and_reset()
    {
        using var t = new TestHost(new MenuProbe());

        t.Press(KeyCode.Plus, KeyModifiers.Ctrl);
        t.Press(KeyCode.Equal, KeyModifiers.Ctrl);
        Assert.Equal(1.2f, t.Ui.Zoom);

        for (int i = 0; i < 40; i++) t.Press(KeyCode.Minus, KeyModifiers.Ctrl);
        Assert.Equal(UiHost.MinZoom, t.Ui.Zoom);

        t.Press(KeyCode.D0, KeyModifiers.Ctrl);
        Assert.Equal(1f, t.Ui.Zoom);
    }

    [Fact]
    public void Zoom_shortcuts_can_be_disabled()
    {
        using var t = new TestHost(new MenuProbe());
        t.Ui.ZoomShortcuts = false;

        Assert.False(t.Press(KeyCode.Plus, KeyModifiers.Ctrl));
        Assert.Equal(1f, t.Ui.Zoom);
    }
}
