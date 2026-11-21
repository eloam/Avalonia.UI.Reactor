// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A scope whose content is several views (a component, a <c>ForEach</c> row, an <c>Observe</c> block) places them among
/// the children of its container, as SwiftUI does: a fragment.
/// </summary>
public class FragmentTests
{
    [AvaloniaFact]
    public void The_views_of_a_block_take_their_places_among_the_children_of_the_row()
    {
        using var t = new ReactorTestHost(new FragmentProbe());

        Rect before = t.BoundsOf(t.TextShowing("avant")), a = t.BoundsOf(t.TextShowing("a"));
        Rect b = t.BoundsOf(t.TextShowing("b")), after = t.BoundsOf(t.TextShowing("après"));
        Assert.Equal(a.Y, b.Y);
        Assert.True(before.X < a.X && a.X < b.X && b.X < after.X);
    }

    [AvaloniaFact]
    public void A_block_re_evaluated_alone_updates_its_views_and_keeps_the_controls_around_it()
    {
        var probe = new FragmentProbe();
        using var t = new ReactorTestHost(probe);
        TextBlock before = t.TextShowing("avant"), a = t.TextShowing("a"), after = t.TextShowing("après");

        probe.Show = true;
        t.Settle();

        Assert.Equal(1, t.Stats.Evaluated);
        Assert.Same(before, t.TextShowing("avant"));
        Assert.Same(a, t.TextShowing("a"));
        Assert.Same(after, t.TextShowing("après"));
        Rect x = t.BoundsOf(t.TextShowing("x"));
        Assert.True(t.BoundsOf(a).X < x.X && x.X < t.BoundsOf(t.TextShowing("b")).X);
    }

    [AvaloniaFact]
    public void A_field_in_a_block_keeps_its_focus_when_a_view_appears_before_it_in_the_block()
    {
        var probe = new FragmentFieldProbe();
        using var t = new ReactorTestHost(probe);
        TextBox field = t.Field;
        TextBlock after = t.TextShowing("après");
        t.Click(field);
        t.Type("abc");

        probe.HasError = true;
        t.Settle();

        Assert.True(t.HasText("Erreur"));
        Assert.Same(field, t.Field);
        Assert.True(field.IsFocused);
        Assert.Same(after, t.TextShowing("après"));
    }

    [AvaloniaFact]
    public void A_block_in_a_place_for_one_control_stacks_its_views()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => Observe(() => Group(Text("a"), Text("b")))));

        Assert.True(t.BoundsOf(t.TextShowing("b")).Y > t.BoundsOf(t.TextShowing("a")).Y);
    }

    [AvaloniaFact]
    public void The_modifiers_of_a_block_apply_to_each_of_its_views()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => HStack(Observe(() => Group(Text("a"), Text("b"))).Padding(4))));

        Rect a = t.BoundsOf(t.TextShowing("a")), b = t.BoundsOf(t.TextShowing("b"));
        Assert.Equal(a.Y, b.Y);
        Assert.Equal(4, a.X);
        Assert.True(b.X >= a.Right + 8);
    }

    [AvaloniaFact]
    public void The_views_of_each_row_of_a_list_take_their_places_in_the_row()
    {
        int[] items = [1, 2];
        using var t = new ReactorTestHost(new ViewProbe(() =>
            HStack(ForEach(items, item => Group(Text($"n{item}"), Text($"m{item}"))))));

        string[] texts = ["n1", "m1", "n2", "m2"];
        Rect[] bounds = [.. texts.Select(text => t.BoundsOf(t.TextShowing(text)))];
        Assert.All(bounds, rect => Assert.Equal(bounds[0].Y, rect.Y));
        Assert.True(bounds[0].X < bounds[1].X && bounds[1].X < bounds[2].X && bounds[2].X < bounds[3].X);
    }

    [AvaloniaFact]
    public void The_views_of_a_component_whose_body_is_a_group_take_their_places_in_the_row()
    {
        var component = new GroupBodyProbe();
        using var t = new ReactorTestHost(new ViewProbe(() => HStack(Text("avant"), component)));

        Rect before = t.BoundsOf(t.TextShowing("avant")), c1 = t.BoundsOf(t.TextShowing("c1")), c2 = t.BoundsOf(t.TextShowing("c2"));
        Assert.Equal(c1.Y, c2.Y);
        Assert.True(before.X < c1.X && c1.X < c2.X);
    }
}
