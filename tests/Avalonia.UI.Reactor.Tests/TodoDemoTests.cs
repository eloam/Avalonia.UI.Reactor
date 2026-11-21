// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;
using Avalonia.Input;
using TodoDemo;
using TodoDemo.Components;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Widgets;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// User scenarios on the sample application, rendered by the Avalonia engine.
/// </summary>
public class TodoDemoTests
{
    private static ReactorTestHost Demo() => new(new TodoApp(), 560, 720, DemoThemes.Create());

    [AvaloniaFact]
    public void Counter_increments_decrements_and_resets()
    {
        using ReactorTestHost t = Demo();

        t.ClickButton("+");
        t.ClickButton("+");
        t.ClickButton("−");
        Assert.True(t.HasText("1"));
        Assert.True(t.HasButton("Reset"));

        t.ClickButton("Reset");
        Assert.True(t.HasText("0"));
        Assert.False(t.HasButton("Reset"));
    }

    [AvaloniaFact]
    public void Typing_then_Enter_adds_a_task_and_clears_the_field()
    {
        using ReactorTestHost t = Demo();
        t.Click(t.Field);

        t.Type("Publish the release");
        t.Press(Key.Enter, PhysicalKey.Enter);

        Assert.True(t.HasText("Publish the release"));
        Assert.Equal("", t.Field.Text);
        Assert.True(t.HasText("2 left"));
    }

    [AvaloniaFact]
    public void Backspace_and_arrows_edit_at_the_caret()
    {
        using ReactorTestHost t = Demo();
        t.Click(t.Field);

        t.Type("abd");
        t.Press(Key.Left, PhysicalKey.ArrowLeft);
        t.Type("c");
        t.Press(Key.End, PhysicalKey.End);
        t.Press(Key.Back, PhysicalKey.Backspace);

        Assert.Equal("abc", t.Field.Text);
    }

    [AvaloniaFact]
    public void Tapping_a_row_toggles_the_task()
    {
        using ReactorTestHost t = Demo();

        t.Click(t.TextShowing("Add a mobile backend"));
        Assert.True(t.HasText("0 left"));
        Assert.All(t.Controls.OfType<ReactorCheckBox>(), box => Assert.True(box.IsChecked));

        t.Click(t.TextShowing("Add a mobile backend"));
        Assert.True(t.HasText("1 left"));
    }

    [AvaloniaFact]
    public void Async_action_shows_progress_then_result()
    {
        using ReactorTestHost t = Demo();

        t.ClickButton("Idea");
        Assert.True(t.HasButton("…"));

        t.WaitUntil(() => t.HasButton("Idea"));
        Assert.True(t.HasText("2 left"));
    }

    [AvaloniaFact]
    public void Filters_and_purge()
    {
        using ReactorTestHost t = Demo();

        t.ClickButton("Done");
        Assert.True(t.HasText("Write the layout engine"));
        Assert.False(t.HasText("Add a mobile backend"));

        t.ClickButton("Active");
        Assert.False(t.HasText("Write the layout engine"));
        Assert.True(t.HasText("Add a mobile backend"));

        t.ClickButton("All");
        t.ClickButton("Purge");
        Assert.False(t.HasText("Write the layout engine"));
        Assert.True(t.HasText("Add a mobile backend"));
        Assert.False(t.HasButton("Purge"));
    }

    [AvaloniaFact]
    public void Changing_theme_keeps_child_component_state()
    {
        using ReactorTestHost t = Demo();
        t.ClickButton("+");

        t.ClickButton("Dark");
        Assert.Equal(Theme.Dark.Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());
        Assert.True(t.HasText("1"));

        t.ClickButton("+");
        Assert.True(t.HasText("2"));
    }

    [AvaloniaFact]
    public void The_theme_choice_is_plain_state()
    {
        Themes catalog = DemoThemes.Create();
        using var t = new ReactorTestHost(new TodoApp(), 560, 720, catalog);
        foreach (string? label in new[] { "Standard", "Sable", "System", "Light", "Dark" })
            Assert.True(t.HasButton(label), label);
        ThemeFamily sable = catalog.FamilyNamed("Sable")!;

        t.ClickButton("Sable");
        Assert.Equal(sable.For(ColorScheme.Light).Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());

        t.ClickButton("Dark");
        Assert.Equal(sable.For(ColorScheme.Dark).Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());
    }

    [AvaloniaFact]
    public void Only_what_depends_on_the_changed_state_is_recomputed()
    {
        using ReactorTestHost t = Demo();

        t.ClickButton("+");
        // The counter card only.
        Assert.Equal(1, t.Stats.Evaluated);
        // The task list is kept as it is.
        Assert.True(t.Stats.Reused >= 1);

        t.Click(t.Field);
        t.Type("x");
        // The input row only.
        Assert.Equal(1, t.Stats.Evaluated);

        t.Click(t.TextShowing("Add a mobile backend"));
        // The checked row, the "N remaining" footer and the progress ring.
        Assert.Equal(3, t.Stats.Evaluated);
    }

    [AvaloniaFact]
    public void The_progress_ring_shows_its_percentage()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(TodoDemo.Widgets.ProgressRing(1, 4))));

        Assert.True(t.HasText("25%"));
    }
}
