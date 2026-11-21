// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.Controls;
using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A field is a real Avalonia TextBox bound to state: input goes to the state, and the state decides what is shown.
/// </summary>
public class TextFieldTests
{
    [AvaloniaFact]
    public void Typing_updates_the_bound_state_and_Enter_submits()
    {
        var probe = new FormProbe();
        using var t = new ReactorTestHost(probe);
        t.Click(t.Field);

        t.Type("Écrire les tests");
        Assert.Equal("Écrire les tests", probe.Draft);

        t.Press(Key.Enter, PhysicalKey.Enter);
        Assert.True(t.HasText("Écrire les tests"));
        Assert.Equal("", probe.Draft);
        Assert.Equal("", t.Field.Text);
    }

    [AvaloniaFact]
    public void The_state_follows_the_text_at_once_so_an_action_right_after_sees_it()
    {
        var probe = new FormProbe();
        using var t = new ReactorTestHost(probe);

        // Text changed, then an action, in the same task of the user interface thread: nothing pending runs in between.
        t.Field.Text = "tout de suite";
        t.ButtonLabeled("Ajouter").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        t.Settle();

        Assert.True(t.HasText("tout de suite"));
    }

    [AvaloniaFact]
    public void The_field_keeps_its_control_focus_and_caret_while_the_state_changes()
    {
        using var t = new ReactorTestHost(new FormProbe());
        TextBox field = t.Field;
        t.Click(field);

        t.Type("abc");

        Assert.Same(field, t.Field);
        Assert.True(field.IsFocused);
        Assert.Equal(3, field.CaretIndex);
    }

    [AvaloniaFact]
    public void The_state_has_the_last_word_on_what_the_field_shows()
    {
        var probe = new ShoutProbe();
        using var t = new ReactorTestHost(probe);
        t.Click(t.Field);

        t.Type("ab");

        Assert.Equal("AB", probe.Shout);
        Assert.Equal("AB", t.Field.Text);
    }

    [AvaloniaFact]
    public void A_field_whose_state_ignores_the_input_keeps_showing_the_state()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(TextField("fixe", onChange: null))));
        t.Click(t.Field);

        t.Type("x");

        Assert.Equal("fixe", t.Field.Text);
    }

    [AvaloniaFact]
    public void A_long_text_scrolls_inside_the_field_instead_of_widening_it()
    {
        using var t = new ReactorTestHost(new FormProbe());
        TextFieldTheme style = Theme.Light.TextField;
        double width = t.Field.Bounds.Width;
        Assert.Equal(style.MinWidth + style.Padding.Horizontal, width);
        t.Click(t.Field);

        t.Type(new string('m', 80));

        Assert.Equal(width, t.Field.Bounds.Width);
    }
}
