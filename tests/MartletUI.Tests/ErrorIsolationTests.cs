// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;
using MartletUI.State;
using MartletUI.Tests.Probes;
using MartletUI.Views;
using static MartletUI.AvaloniaViews;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// An exception in a view stays in its scope: it is reported, the scope keeps its previous content (or shows nothing),
/// and the rest of the window is updated.
/// </summary>
public class ErrorIsolationTests
{
    private const string Message = "The view failed.";

    private readonly CounterProbe _failing = new();
    private readonly CounterProbe _other = new();

    private bool Fails => _failing.Count == 1;

    [AvaloniaFact]
    public void A_sibling_of_a_component_that_throws_is_updated()
    {
        ViewProbe bad = Bad();
        var good = new ViewProbe(() => Text($"other {_other.Count}"));
        using var t = new MartletTestHost(new ViewProbe(() => VStack((View)bad, (View)good)));

        _failing.Count = 1;
        _other.Count = 1;
        t.Settle();

        Assert.Equal(["value 0", "other 1"], TextsOf(t));
        Assert.Equal(Message, Assert.Single(t.Errors).Message);
    }

    [AvaloniaFact]
    public void A_component_that_throws_is_computed_again_when_its_state_changes()
    {
        using var t = new MartletTestHost(Bad());
        _failing.Count = 1;
        t.Settle();
        Assert.Equal(["value 0"], TextsOf(t));

        _failing.Count = 2;
        t.Settle();

        Assert.Equal(["value 2"], TextsOf(t));
        Assert.Single(t.Errors);
    }

    [AvaloniaFact]
    public void A_root_that_throws_when_the_window_opens_shows_nothing_until_its_state_changes()
    {
        _failing.Count = 1;
        using var t = new MartletTestHost(Bad());
        Assert.Empty(TextsOf(t));
        Assert.Single(t.Errors);

        _failing.Count = 2;
        t.Settle();

        Assert.Equal(["value 2"], TextsOf(t));
    }

    [AvaloniaFact]
    public void A_child_that_throws_when_it_appears_shows_nothing_among_its_siblings()
    {
        ViewProbe bad = Bad();
        _failing.Count = 1;
        using var t = new MartletTestHost(new ViewProbe(() => VStack(
            Text($"other {_other.Count}"),
            _other.Count == 1 ? (View)bad : null,
            Text("end"))));

        _other.Count = 1;
        t.Settle();

        Assert.Equal(["other 1", "end"], TextsOf(t));
        Assert.Single(t.Errors);
    }

    [AvaloniaFact]
    public void A_row_that_throws_does_not_stop_the_other_rows()
    {
        var items = new StateList<string> { "a", "b" };
        using var t = new MartletTestHost(new ViewProbe(() => VStack(
            ForEach(items, item => item == "bad" ? throw new InvalidOperationException(Message) : Text(item), item => item))));

        items.Insert(0, "bad");
        items.Add("c");
        t.Settle();

        Assert.Equal(["a", "b", "c"], TextsOf(t));
        Assert.Single(t.Errors);
    }

    [AvaloniaFact]
    public void A_content_that_cannot_be_displayed_is_replaced_by_the_previous_one()
    {
        var items = new StateList<string> { "a", "b" };
        var list = new ViewProbe(() => VStack(
            Text($"{items.Count} items"),
            VStack(ForEach(items, item => Text(item), item => item))));
        var good = new ViewProbe(() => Text($"other {_other.Count}"));
        using var t = new MartletTestHost(new ViewProbe(() => VStack((View)list, (View)good)));

        // Two rows with the same key.
        items.Add("a");
        _other.Count = 1;
        t.Settle();

        // The first text was already updated when the list failed: it takes its previous value again.
        Assert.Equal(["2 items", "a", "b", "other 1"], TextsOf(t));
        Assert.Contains("the key 'a'", Assert.Single(t.Errors).Message, StringComparison.Ordinal);

        items[2] = "c";
        t.Settle();

        Assert.Equal(["3 items", "a", "b", "c", "other 1"], TextsOf(t));
        Assert.Single(t.Errors);
    }

    [AvaloniaFact]
    public void A_content_that_cannot_be_displayed_at_first_shows_nothing()
    {
        var list = new ViewProbe(() => VStack(Text("a").Key("same"), Text("b").Key("same")));
        using var t = new MartletTestHost(new ViewProbe(() => VStack((View)list, Text("end"))));

        Assert.Equal(["end"], TextsOf(t));
        Assert.Single(t.Errors);
    }

    [AvaloniaFact]
    public void A_field_keeps_its_control_and_its_focus_when_its_view_throws()
    {
        var form = new FormProbe();
        using var t = new MartletTestHost(new ViewProbe(() => Fails
            ? throw new InvalidOperationException(Message)
            : VStack(TextField(form.Bind.Draft))));
        Avalonia.Controls.TextBox field = t.Field;
        field.Focus();

        _failing.Count = 1;
        t.Settle();

        Assert.Same(field, t.Field);
        Assert.True(field.IsFocused);
    }

    [AvaloniaFact]
    public void The_lifecycles_of_a_view_that_throws_go_on()
    {
        var log = new List<string>();
        using (var t = new MartletTestHost(new ViewProbe(() => Fails
            ? throw new InvalidOperationException(Message)
            : Text("a").OnAppear(() => log.Add("appear")).OnDisappear(() => log.Add("disappear")))))
        {
            _failing.Count = 1;
            t.Settle();
            _failing.Count = 2;
            t.Settle();
            Assert.Equal(["appear"], log);
        }

        Assert.Equal(["appear", "disappear"], log);
    }

    [AvaloniaFact]
    public void The_lifecycles_of_a_content_that_was_not_displayed_never_start()
    {
        var log = new List<string>();
        View Row(string key) => Text(key).Key(key).OnAppear(() => log.Add($"+{key}")).OnDisappear(() => log.Add($"-{key}"));
        using var t = new MartletTestHost(new ViewProbe(() => Fails ? VStack(Row("new"), Row("a"), Row("a")) : VStack(Row("a"))));
        t.Settle();

        _failing.Count = 1;
        t.Settle();
        Assert.Equal(["+a"], log);

        _failing.Count = 2;
        t.Settle();

        Assert.Equal(["+a"], log);
    }

    [AvaloniaFact]
    public void The_lifecycles_of_a_content_that_is_replaced_by_nothing_end()
    {
        var log = new List<string>();
        bool broken = false;
        using var t = new MartletTestHost(new ViewProbe(() => VStack(
            Text("a").OnAppear(() => log.Add("appear")).OnDisappear(() => log.Add("disappear")),
            Text($"{_other.Count}"),
            Button("x", () => { }).Template(_ => broken ? throw new InvalidOperationException(Message) : Text("x")))));
        t.Settle();
        Assert.Equal(["appear"], log);

        // Neither the new content nor the previous one can be displayed.
        broken = true;
        _other.Count = 1;
        t.Settle();

        Assert.Empty(TextsOf(t));
        Assert.Equal(["appear", "disappear"], log);
        Assert.Equal(2, t.Errors.Count);

        broken = false;
        _other.Count = 2;
        t.Settle();

        Assert.Equal(["a", "2"], TextsOf(t));
        Assert.Equal(["appear", "disappear", "appear"], log);
    }

    [AvaloniaFact]
    public void The_lifecycles_of_a_component_kept_by_the_previous_content_go_on()
    {
        var log = new List<string>();
        var child = new ViewProbe(() => Text($"child {_other.Count}").OnAppear(() => log.Add("appear")).OnDisappear(() => log.Add("disappear")));
        using var t = new MartletTestHost(new ViewProbe(() => Fails
            ? VStack((View)child, Text("a").Key("same"), Text("b").Key("same"))
            : VStack((View)child)));
        t.Settle();

        // The component is visited by the content that fails, then by the previous one.
        _failing.Count = 1;
        _other.Count = 1;
        t.Settle();
        t.Settle();

        Assert.Equal(["child 1"], TextsOf(t));
        Assert.Equal(["appear"], log);

        _other.Count = 2;
        t.Settle();

        Assert.Equal(["child 2"], TextsOf(t));
        Assert.Equal(["appear"], log);
    }

    [AvaloniaFact]
    public void The_lifecycles_of_a_content_that_fails_without_being_computed_again_end()
    {
        var log = new List<string>();
        bool broken = false;
        using var t = new MartletTestHost(new ViewProbe(() => VStack(
            Button("x", () => { }).Template(_ => broken ? throw new InvalidOperationException(Message) : Text("x")),
            Text("a").OnAppear(() => log.Add("appear")).OnDisappear(() => log.Add("disappear")),
            Observe(() => Text($"{_other.Count}")))));
        t.Settle();

        broken = true;
        _other.Count = 1;
        t.Settle();

        Assert.Empty(TextsOf(t));
        Assert.Equal(["appear", "disappear"], log);
    }

    [AvaloniaFact]
    public void The_scopes_of_a_content_that_was_not_displayed_are_released()
    {
        ViewProbe child = new(() => Text("child"));
        using var t = new MartletTestHost(new ViewProbe(() => Fails
            ? VStack((View)child, Text("a").Key("same"), Text("b").Key("same"))
            : Text("ok")));
        Assert.Equal(1, t.Stats.Scopes);

        _failing.Count = 1;
        t.Settle();

        Assert.Equal(["ok"], TextsOf(t));
        Assert.Equal(1, t.Stats.Scopes);
    }

    [AvaloniaFact]
    public void A_native_update_that_throws_does_not_stop_its_siblings()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(
            Native(
                () => new Avalonia.Controls.Slider(),
                slider => slider.Value = Fails ? throw new InvalidOperationException(Message) : _failing.Count),
            Text($"other {_other.Count}"))));

        _failing.Count = 1;
        t.Settle();
        _other.Count = 1;
        t.Settle();
        Assert.Equal(["other 1"], TextsOf(t));
        Assert.NotEmpty(t.Errors);

        _failing.Count = 5;
        t.Settle();

        Assert.Equal(5, t.Controls.OfType<Avalonia.Controls.Slider>().Single().Value);
    }

    [AvaloniaFact]
    public void A_drawing_that_throws_does_not_stop_the_rendering_of_the_window()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(
                Canvas(drawing =>
                {
                    drawing.FillRect(drawing.Bounds, Fails ? throw new InvalidOperationException(Message) : "#FF0000");
                }).Size(50, 50),
                VStack().Size(50, 50).Background(_other.Count == 0 ? "#00FF00" : "#0000FF"))
            .Spacing(0)
            .Padding(0)));
        Assert.Equal(Avalonia.Media.Colors.Red, t.PixelAt(25, 25));

        _failing.Count = 1;
        _other.Count = 1;
        t.Settle();

        Assert.Equal(Avalonia.Media.Colors.Blue, t.PixelAt(25, 75));
        Assert.Contains(t.Errors, error => error.Message == Message);

        _failing.Count = 2;
        t.Settle();

        Assert.Equal(Avalonia.Media.Colors.Red, t.PixelAt(25, 25));
    }

    private static List<string?> TextsOf(MartletTestHost t) => [.. t.Texts.Select(text => text.Text)];

    private ViewProbe Bad() => new(() => Fails ? throw new InvalidOperationException(Message) : Text($"value {_failing.Count}"));
}
