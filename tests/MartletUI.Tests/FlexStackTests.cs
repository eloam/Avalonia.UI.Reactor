// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using MartletUI.Panels;
using MartletUI.Tests.Probes;
using MartletUI.Theming;
using MartletUI.Views;
using MartletUI.Widgets;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// Stacks follow the rules of SwiftUI (window of 400 × 300).
/// </summary>
public class FlexStackTests
{
    [AvaloniaFact]
    public void A_spacer_pushes_the_following_views_to_the_end()
    {
        using var t = new MartletTestHost(new ViewProbe(() => HStack(Text("a"), Spacer(), Button("b"))));

        Assert.Equal(0, t.BoundsOf(t.TextShowing("a")).X);
        Assert.Equal(400, t.BoundsOf(t.ButtonLabeled("b")).Right);
    }

    [AvaloniaFact]
    public void Grow_shares_the_remaining_space_in_proportion()
    {
        using var t = new MartletTestHost(new ViewProbe(() => HStack(Spacer().Grow(1), Spacer().Grow(3)).Spacing(0)));

        Controls spacers = t.Controls.OfType<FlexStack>().Single().Children;
        Assert.Equal(100, spacers[0].Bounds.Width);
        Assert.Equal(300, spacers[1].Bounds.Width);
    }

    [AvaloniaFact]
    public void A_vertical_stack_aligns_its_children_to_the_start_and_spaces_them_by_the_theme()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Text("court"), Text("un texte plus long"))));

        Rect first = t.BoundsOf(t.TextShowing("court"));
        Rect second = t.BoundsOf(t.TextShowing("un texte plus long"));
        Assert.Equal(0, first.X);
        Assert.Equal(0, second.X);
        Assert.True(first.Width < second.Width);
        Assert.Equal(first.Bottom + Theme.Light.Spacing, second.Y);
    }

    [AvaloniaFact]
    public void A_horizontal_stack_centers_its_children_vertically()
    {
        using var t = new MartletTestHost(new ViewProbe(() => HStack(Text("a"), Button("b"))));

        Assert.Equal(t.BoundsOf(t.ButtonLabeled("b")).Center.Y, t.BoundsOf(t.TextShowing("a")).Center.Y, tolerance: 1);
    }

    [AvaloniaFact]
    public void FillWidth_stretches_a_child_across_the_stack()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Text("a").FillWidth(), Text("b"))));

        Assert.Equal(400, t.TextShowing("a").Bounds.Width);
        Assert.True(t.TextShowing("b").Bounds.Width < 400);
    }

    [AvaloniaFact]
    public void A_stack_with_a_spacer_stretches_without_being_told()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(HStack(Text("a"), Spacer(), Text("b")), Text("c"))));

        Assert.Equal(400, t.BoundsOf(t.TextShowing("b")).Right);
    }

    [AvaloniaFact]
    public void Padding_background_border_and_radius_are_drawn_by_an_enclosing_surface()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
            VStack(Text("a")).Padding(16).Background(ColorRole.Surface).Border(ColorRole.Border, 2).Radius(10)));

        Surface surface = Assert.IsType<Surface>(t.Host.Content);
        Assert.Equal(new Thickness(16), surface.Padding);
        Assert.Equal(new Thickness(2), surface.BorderThickness);
        Assert.Equal(new CornerRadius(10), surface.CornerRadius);
        Assert.Equal(Theme.Light.Palette.Surface.ToAvaloniaColor(), surface.Background.ColorOf());
        // The border takes its own space, as in CSS and in Avalonia controls: the content starts after it.
        Assert.Equal(new Point(18, 18), t.BoundsOf(t.TextShowing("a")).TopLeft);
    }

    [AvaloniaFact]
    public void A_stack_without_a_flexible_child_keeps_its_natural_width()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(HStack(Text("a"), Text("b")))));

        Assert.True(t.BoundsOf(t.TextShowing("b")).Right < 100);
    }

    [AvaloniaFact]
    public void An_explicit_width_wins_over_flexible_children()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(HStack(Text("a"), Spacer(), Text("b")).Width(100))));

        Assert.Equal(100, t.BoundsOf(t.TextShowing("b")).Right);
    }

    [AvaloniaFact]
    public void A_vertical_spacer_stretches_its_column()
    {
        using var t = new MartletTestHost(new ViewProbe(() => HStack(VStack(Text("haut"), Spacer(), Text("bas")))));

        Assert.Equal(300, t.BoundsOf(t.TextShowing("bas")).Bottom);
    }

    [AvaloniaFact]
    public void Spacing_is_a_modifier()
    {
        using var t = new MartletTestHost(new ViewProbe(() => VStack(Text("a"), Text("b")).Spacing(5)));

        Assert.Equal(t.BoundsOf(t.TextShowing("a")).Bottom + 5, t.BoundsOf(t.TextShowing("b")).Y);
    }

    private static readonly string[] s_letters = ["x", "y"];
    private static readonly string[] s_inOrder = ["début", "x", "y", "fin"];

    [AvaloniaFact]
    public void ForEach_mixes_with_other_children_and_takes_the_spacing_of_the_stack()
    {
        using var t = new MartletTestHost(new ViewProbe(() =>
            VStack(Text("début"), ForEach(s_letters, s => Text(s)), Text("fin")).Spacing(10)));

        double[] tops = [.. s_inOrder.Select(s => t.BoundsOf(t.TextShowing(s)).Y)];
        Assert.Equal(tops.Order(), tops);
        Assert.Equal(t.BoundsOf(t.TextShowing("début")).Bottom + 10, t.BoundsOf(t.TextShowing("x")).Y);
    }

    [AvaloniaFact]
    public void Nested_groups_are_unfolded_in_place()
    {
        using var t = new MartletTestHost(new ViewProbe(() => HStack(Text("a"), Group(Text("b"), Group(Text("c"))))));

        Rect a = t.BoundsOf(t.TextShowing("a")), b = t.BoundsOf(t.TextShowing("b")), c = t.BoundsOf(t.TextShowing("c"));
        // Unfolded in the row: side by side, at the same height.
        Assert.Equal(a.Y, c.Y);
        Assert.True(b.X > a.X);
        Assert.True(c.X > b.X);
    }

    [AvaloniaFact]
    public void A_modifier_of_a_group_applies_to_each_child_which_stays_in_place()
    {
        using var t = new MartletTestHost(new ViewProbe(() => HStack(Group(Text("a"), Text("b")).Padding(4))));

        Rect a = t.BoundsOf(t.TextShowing("a")), b = t.BoundsOf(t.TextShowing("b"));
        // Side by side in the row, as SwiftUI does, each with its own padding.
        Assert.Equal(a.Y, b.Y);
        Assert.Equal(4, a.X);
        Assert.True(b.X >= a.Right + 8);
    }

    [AvaloniaFact]
    public void A_child_keeps_its_own_modifier_over_the_one_of_its_group()
    {
        using var t = new MartletTestHost(new ViewProbe(() => HStack(Group(Text("a").Padding(10), Text("b")).Padding(4))));

        Assert.Equal(10, t.BoundsOf(t.TextShowing("a")).X);
    }

    [AvaloniaFact]
    public void A_group_that_no_container_unfolds_stacks_its_children_with_its_modifiers()
    {
        using var t = new MartletTestHost(new ViewProbe(() => Group(Text("a"), Text("b")).Padding(4)));

        Rect a = t.BoundsOf(t.TextShowing("a")), b = t.BoundsOf(t.TextShowing("b"));
        Assert.Equal(4, a.X);
        Assert.Equal(4, b.X);
        Assert.True(b.Y >= a.Bottom + 8);
    }
}
