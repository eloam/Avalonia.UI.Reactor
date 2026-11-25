// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;
using MartletUI.State;
using MartletUI.Tests.Probes;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// A state list changes from any thread, even while a view or another thread reads it.
/// </summary>
public class StateListTests
{
    private const int Changes = 3000;
    private const int MaxItems = 30;

    [AvaloniaFact]
    public void A_list_changed_from_another_thread_while_it_is_displayed_is_updated()
    {
        var items = new StateList<int>();
        using var t = new MartletTestHost(new ViewProbe(() => VStack(ForEach(items, item => Text($"{item}"), item => item))));

        WhileChanging(items, t.Settle);
        t.Settle();

        Assert.Empty(t.Errors);
        Assert.Equal(items.Select(item => $"{item}"), t.Texts.Select(text => text.Text));
    }

    [AvaloniaFact]
    public void The_bindings_of_a_list_changed_from_another_thread_are_displayed()
    {
        var items = new StateList<int>();
        using var t = new MartletTestHost(new ViewProbe(() => VStack(ForEach(items.Bind, item => Text($"{item.Value}")))));

        WhileChanging(items, t.Settle);
        t.Settle();

        Assert.Empty(t.Errors);
        Assert.Equal(items.Select(item => $"{item}"), t.Texts.Select(text => text.Text));
    }

    [Fact]
    public void A_list_is_read_with_LINQ_while_another_thread_changes_it()
    {
        var items = new StateList<int>();

        WhileChanging(items, () =>
        {
            // The items are added in order: a copy taken while the list changes is in order too.
            List<int> copy = [.. items];
            Assert.Equal(copy.Order(), copy);
            Assert.True(copy.Count <= MaxItems + 1);
            Assert.Equal(copy.Count, copy.Distinct().Count());
            _ = items.IndexOf(0);
            _ = items.Contains(0);
        });
    }

    [Fact]
    public void Changes_made_by_several_threads_are_all_kept()
    {
        var items = new StateList<int>();

        Parallel.For(0, Changes, items.Add);

        Assert.Equal(Enumerable.Range(0, Changes), items.Order());
    }

    [Fact]
    public void A_change_made_during_an_enumeration_does_not_affect_it()
    {
        StateList<string> items = ["a", "b"];
        var seen = new List<string>();

        foreach (string item in items)
        {
            seen.Add(item);
            items.Remove(item);
            items.Add(item + item);
        }

        Assert.Equal(["a", "b"], seen);
        Assert.Equal(["aa", "bb"], items);
    }

    [Fact]
    public void A_list_can_be_added_to_itself()
    {
        StateList<string> items = ["a", "b"];

        items.AddRange(items);

        Assert.Equal(["a", "b", "a", "b"], items);
    }

    [Fact]
    public void Every_change_is_seen_by_the_next_read()
    {
        StateList<string> items = ["a"];
        Assert.Equal(["a"], items);

        items.Add("b");
        Assert.Equal(["a", "b"], items);
        items.AddRange(["c", "d"]);
        Assert.Equal(["a", "b", "c", "d"], items);
        items.Insert(0, "e");
        Assert.Equal(["e", "a", "b", "c", "d"], items);
        items[1] = "f";
        Assert.Equal(["e", "f", "b", "c", "d"], items);
        items.Move(0, 2);
        Assert.Equal(["f", "b", "e", "c", "d"], items);
        Assert.True(items.Remove("b"));
        Assert.Equal(["f", "e", "c", "d"], items);
        Assert.False(items.Remove("b"));
        items.RemoveAt(0);
        Assert.Equal(["e", "c", "d"], items);
        Assert.Equal(1, items.RemoveAll(item => item == "c"));
        Assert.Equal(["e", "d"], items);
        Assert.Equal(1, items.IndexOf("d"));
        Assert.Contains("e", items);
        items.Bind[0].Set("g");
        Assert.Equal(["g", "d"], items);
        items.Clear();
        Assert.Empty(items);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(0, -1)]
    [InlineData(2, 0)]
    public void An_invalid_move_changes_nothing(int from, int to)
    {
        StateList<string> items = ["a", "b"];

        Assert.Throws<ArgumentOutOfRangeException>(() => items.Move(from, to));

        Assert.Equal(["a", "b"], items);
    }

    [Fact]
    public void The_binding_of_a_removed_item_keeps_its_value_and_writes_nothing()
    {
        StateList<string> items = ["a", "b"];
        Binding<string> second = items.Bind[1];

        items.RemoveAt(1);
        second.Set("c");

        Assert.Equal("b", second.Value);
        Assert.Equal(["a"], items);
    }

    // Adds and removes items on another thread while this one reads the list.
    private static void WhileChanging(StateList<int> items, Action read)
    {
        Exception? failure = null;
        var writer = new Thread(() =>
        {
            try
            {
                for (int i = 0; i < Changes; i++)
                {
                    items.Add(i);
                    if (items.Count > MaxItems)
                    {
                        items.RemoveAt(0);
                    }
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        writer.Start();
        while (writer.IsAlive)
        {
            read();
        }

        Assert.Null(failure);
    }
}
