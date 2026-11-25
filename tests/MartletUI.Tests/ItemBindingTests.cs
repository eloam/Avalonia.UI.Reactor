// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using MartletUI.State;
using MartletUI.Tests.Probes;
using MartletUI.Views;

namespace MartletUI.Tests;

/// <summary>
/// The bindings to the items of a list (<c>StateList.Bind</c>): a row edits its item, as with <c>ForEach($tags)</c>
/// in SwiftUI.
/// </summary>
public class ItemBindingTests
{
    [AvaloniaFact]
    public void A_field_bound_to_an_item_edits_it_and_keeps_its_control_and_focus()
    {
        var probe = new TagsProbe();
        using var t = new MartletTestHost(probe);
        TextBox second = Fields(t)[1];
        t.Click(second);

        t.Type(" clair");

        Assert.Equal(["rouge", "vert clair"], probe.Tags);
        Assert.True(t.HasText("rouge, vert clair"));
        Assert.Same(second, Fields(t)[1]);
        Assert.True(second.IsFocused);
    }

    [AvaloniaFact]
    public void The_rows_follow_the_items_added_and_removed()
    {
        var probe = new TagsProbe();
        using var t = new MartletTestHost(probe);

        probe.Tags.Add("bleu");
        t.Settle();
        Assert.Equal(["rouge", "vert", "bleu"], Fields(t).Select(field => field.Text));

        // The rows are keyed by index: the first row now shows the item that moved up.
        probe.Tags.RemoveAt(0);
        t.Settle();
        Assert.Equal(["vert", "bleu"], Fields(t).Select(field => field.Text));
    }

    [Fact]
    public void The_rows_of_item_bindings_are_keyed_by_their_list_and_their_index()
    {
        StateList<string> tags = ["rouge", "vert"];
        StateList<string> others = ["rouge"];

        string?[] first = Keys(Ui.ForEach(tags.Bind, tag => Ui.Text(tag.Value)));
        string?[] again = Keys(Ui.ForEach(tags.Bind, tag => Ui.Text(tag.Value)));
        string?[] other = Keys(Ui.ForEach(others.Bind, tag => Ui.Text(tag.Value)));

        Assert.Equal(first, again);
        Assert.Equal(2, first.Distinct().Count());
        Assert.DoesNotContain(other[0], first);
    }

    [Fact]
    public void Writing_through_the_binding_of_a_removed_item_does_nothing()
    {
        StateList<string> tags = ["rouge", "vert"];
        Binding<string> second = tags.Bind[1];

        tags.RemoveAt(1);
        second.Set("bleu");

        Assert.Equal(["rouge"], tags);
    }

    private static TextBox[] Fields(MartletTestHost t) => [.. t.Controls.OfType<TextBox>()];

    private static string?[] Keys(GroupView rows) => [.. rows.Children.Select(row => row.Key)];
}
