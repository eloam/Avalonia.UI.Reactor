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
/// A view only reads state: changing the state that a view depends on while it is computed throws, instead of starting
/// its update again without end.
/// </summary>
/// <remarks>
/// The writes are hidden from the analyzer (MVU018) in lambdas of the tests. Each is limited to
/// <see cref="MaxWrites"/>, so that a test without the guard fails instead of running forever.
/// </remarks>
public class PureViewTests
{
    private const int MaxWrites = 50;

    private readonly CounterProbe _counter = new();
    private int _writes;
    private bool _writing;

    [AvaloniaFact]
    public void A_body_that_changes_the_state_it_reads_is_stopped_at_the_first_write()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
        {
            string text = $"{_counter.Count}";
            Write(() => _counter.Count++);
            return Text(text);
        }));
        _writing = true;

        _counter.Count = 10;
        t.Settle();

        Assert.Equal(1, _writes);
        InvalidOperationException error = Assert.IsType<InvalidOperationException>(Assert.Single(t.Errors));
        Assert.Equal(
            "The state 'Count' was changed while the view of 'ViewProbe' was being computed, which would compute it " +
            "again without end. Change it in an action, in '.OnAppear(…)' or in '.Task(…)'.",
            error.Message);

        // The write did not happen, and the previous view is still displayed.
        Assert.Equal(10, _counter.Count);
        Assert.True(t.HasText("0"));
    }

    [AvaloniaFact]
    public void The_interface_goes_on_after_a_view_was_stopped()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
        {
            string text = $"{_counter.Count}";
            Write(() => _counter.Count++);
            return Text(text);
        }));
        _writing = true;
        _counter.Count = 10;
        t.Settle();

        _writing = false;
        _counter.Count = 20;
        t.Settle();

        Assert.True(t.HasText("20"));
        Assert.Single(t.Errors);
    }

    [AvaloniaFact]
    public void A_list_changed_by_the_view_that_shows_it_keeps_its_items()
    {
        var list = new StateList<string> { "b", "a" };
        Action[] changes =
        [
            () => list.Add("c"),
            () => list.AddRange(["c"]),
            () => list.Insert(0, "c"),
            () => list[0] = "c",
            () => list.Move(0, 1),
            () => list.Remove("a"),
            () => list.RemoveAt(0),
            () => list.RemoveAll(item => item == "a"),
            list.Clear,
        ];
        var trigger = new CounterProbe();
        Action change = () => { };
        using var t = new MartletTestHost(new ViewProbe(() =>
        {
            string text = $"{trigger.Count} {list.Count}";
            Write(() => change());
            return Text(text);
        }));
        _writing = true;

        for (int i = 0; i < changes.Length; i++)
        {
            change = changes[i];
            trigger.Count++;
            t.Settle();

            Assert.Equal(i + 1, t.Errors.Count);
            Assert.Contains("'StateList<String>'", t.Errors[i].Message, StringComparison.Ordinal);
            Assert.Equal(["b", "a"], list);
        }
    }

    [AvaloniaFact]
    public void Refresh_called_by_the_body_is_stopped()
    {
        var probe = new RefreshProbe();
        var trigger = new CounterProbe();
        using var t = new MartletTestHost(new ViewProbe(() =>
        {
            int count = trigger.Count;
            Write(() => probe.Set(count));
            return VStack(Text($"{count}"), (View)probe);
        }));
        _writing = true;

        trigger.Count = 1;
        t.Settle();

        Assert.Contains("'Refresh' was called while", Assert.Single(t.Errors).Message, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_row_that_changes_the_state_it_reads_is_stopped()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Observe(() =>
        {
            string text = $"{_counter.Count}";
            Write(() => _counter.Count++);
            return Text(text);
        }))));
        _writing = true;

        _counter.Count = 10;
        t.Settle();

        Assert.Equal(1, _writes);
        Assert.Contains("while a view was being computed", Assert.Single(t.Errors).Message, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_drawing_that_changes_the_state_it_reads_is_stopped()
    {
        using var t = new MartletTestHost(new ViewProbe(() => Canvas(drawing =>
        {
            drawing.FillCircle(drawing.Bounds.Center, _counter.Count, "#FF0000");
            Write(() => _counter.Count++);
        }).Size(100, 100)));
        _writing = true;

        _counter.Count = 10;
        t.Settle();
        _ = t.PixelAt(50, 50);
        t.Settle();
        _writing = false;
        _ = t.PixelAt(50, 50);
        t.Settle();

        Assert.Equal(10, _counter.Count);
        Assert.InRange(_writes, 1, 2);
        Assert.NotEmpty(t.Errors);
        Assert.All(t.Errors, error => Assert.Contains("while a view was being computed", error.Message, StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void A_state_that_no_view_reads_can_be_changed_while_a_view_is_computed()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
        {
            // Objects created by the view: nothing depends on them yet.
            var list = new StateList<string> { "a" };
            var counter = new CounterProbe { Count = 3 };
            counter.Count = 4;
            list.Add("b");
            return Text($"{counter.Count} {list.Count}");
        }));

        Assert.True(t.HasText("4 2"));
        Assert.Empty(t.Errors);
    }

    [AvaloniaFact]
    public void Writing_the_same_value_while_a_view_is_computed_is_accepted()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
        {
            _counter.Count = Math.Min(_counter.Count, 5);
            return Text($"{_counter.Count}");
        }));

        _counter.Count = 4;
        t.Settle();

        Assert.True(t.HasText("4"));
        Assert.Empty(t.Errors);
    }

    [AvaloniaFact]
    public void A_state_changed_from_another_thread_while_a_view_is_computed_updates_it()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
        {
            string text = $"{_counter.Count}";
            if (_counter.Count == 1)
            {
                var worker = new Thread(() => _counter.Count = 2);
                worker.Start();
                worker.Join();
            }

            return Text(text);
        }));

        _counter.Count = 1;

        t.WaitUntil(() => t.HasText("2"));
        Assert.Empty(t.Errors);
    }

    [AvaloniaFact]
    public void A_native_control_that_corrects_its_value_while_it_is_updated_changes_the_state()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
            Native(
                () =>
                {
                    var slider = new Avalonia.Controls.Slider { Maximum = 100 };
                    slider.ValueChanged += (_, e) => _counter.Count = (int)e.NewValue;
                    return slider;
                },
                slider => slider.Value = _counter.Count)));

        _counter.Count = 50;
        t.Settle();
        _counter.Count = 150;
        t.Settle();

        Assert.Equal(100, _counter.Count);
        Assert.Empty(t.Errors);
    }

    private void Write(Action write)
    {
        if (_writing && _writes < MaxWrites)
        {
            _writes++;
            write();
        }
    }
}
