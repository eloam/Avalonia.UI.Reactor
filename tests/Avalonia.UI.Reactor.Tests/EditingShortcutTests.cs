using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.Tests.Probes;

namespace Avalonia.UI.Reactor.Tests;

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

        Assert.True(t.Press(KeyCode.V, TestHost.Command));
        t.Ui.TextInput("v"); // caractère éventuellement envoyé par la plateforme avec la touche

        Assert.Equal("[deux lignes]", probe.Draft);
    }

    [Fact]
    public void Ctrl_V_without_a_focused_field_or_clipboard_text_does_nothing()
    {
        var probe = new MenuProbe();
        using var t = new TestHost(probe);
        t.Ui.Clipboard = new FakeClipboard();
        t.Press(KeyCode.V, TestHost.Command);

        t.Click(v => v.Key == "field");
        t.Press(KeyCode.V, TestHost.Command);

        Assert.Equal("", probe.Draft);
    }

    [Fact]
    public void Zoom_shortcuts_step_by_ten_percent_within_bounds_and_reset()
    {
        using var t = new TestHost(new MenuProbe());

        t.Press(KeyCode.Plus, TestHost.Command);
        t.Press(KeyCode.Equal, TestHost.Command);
        Assert.Equal(1.2f, t.Ui.Zoom);

        for (int i = 0; i < 40; i++) t.Press(KeyCode.Minus, TestHost.Command);
        Assert.Equal(UiHost.MinZoom, t.Ui.Zoom);

        t.Press(KeyCode.D0, TestHost.Command);
        Assert.Equal(1f, t.Ui.Zoom);
    }

    [Fact]
    public void Zoom_shortcuts_can_be_disabled()
    {
        using var t = new TestHost(new MenuProbe());
        t.Ui.ZoomShortcuts = false;

        Assert.False(t.Press(KeyCode.Plus, TestHost.Command));
        Assert.Equal(1f, t.Ui.Zoom);
    }
}
