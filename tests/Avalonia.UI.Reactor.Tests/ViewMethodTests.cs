// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;

/// <summary>
/// The scopes of view methods called in blocks, as the subviews of SwiftUI: re-evaluated alone when their state
/// changes, skipped when their caller is re-evaluated with equal arguments; and the <c>Observe</c> block.
/// </summary>
public class ViewMethodTests
{
    [AvaloniaFact]
    public void A_view_method_is_re_evaluated_alone_when_its_state_changes()
    {
        var probe = new ViewMethodProbe();
        using var t = new ReactorTestHost(probe);
        int bodyRuns = probe.BodyRuns;

        probe.Progress = 1;
        t.Settle();

        Assert.True(t.HasText("Progression 1"));
        // The method and the Observe block, which read Progress; not the Body.
        Assert.Equal(2, t.Stats.Evaluated);
        Assert.Equal(bodyRuns, probe.BodyRuns);
    }

    [AvaloniaFact]
    public void A_view_method_is_skipped_when_its_caller_is_re_evaluated_with_equal_arguments()
    {
        var probe = new ViewMethodProbe();
        using var t = new ReactorTestHost(probe);

        probe.Title = 1;
        t.Settle();

        Assert.True(t.HasText("Titre 1"));
        // The Body, and the Observe block, re-evaluated with its parent; the view methods are skipped.
        Assert.Equal(2, t.Stats.Evaluated);
    }

    [AvaloniaFact]
    public void A_view_method_is_re_evaluated_when_its_arguments_change()
    {
        var probe = new ViewMethodProbe();
        using var t = new ReactorTestHost(probe);

        probe.Size = 1;
        t.Settle();

        Assert.True(t.HasText("Parité 1"));
        // The Body, the badge whose argument changed, and the Observe block.
        Assert.Equal(3, t.Stats.Evaluated);

        probe.Size = 3;
        t.Settle();

        // The argument is equal again: the badge is skipped.
        Assert.Equal(2, t.Stats.Evaluated);
    }

    [AvaloniaFact]
    public void Refresh_re_evaluates_the_view_methods_of_the_component()
    {
        var probe = new ViewMethodProbe();
        using var t = new ReactorTestHost(probe);

        probe.ChangeNote("note 2");
        t.Settle();

        Assert.True(t.HasText("note 2"));
    }

    [AvaloniaFact]
    public void A_view_method_keeps_its_control_and_focus_when_a_view_appears_before_it()
    {
        var probe = new ViewMethodProbe();
        using var t = new ReactorTestHost(probe);
        TextBox field = t.Field;
        t.Click(field);
        t.Type("Ada");

        probe.HasError = true;
        t.Settle();

        Assert.True(t.HasText("Erreur"));
        Assert.Same(field, t.Field);
        Assert.True(field.IsFocused);
        Assert.Equal("Ada", probe.Name);
    }

    [AvaloniaFact]
    public void The_modifiers_of_a_view_method_apply_to_its_view()
    {
        using var t = new ReactorTestHost(new ViewMethodProbe());

        FontWeight note = t.Texts.Single(text => text.Text == "note 1").FontWeight;

        Assert.True(note > t.Texts.Single(text => text.Text == "Titre 0").FontWeight);
    }

    [AvaloniaFact]
    public void An_Observe_block_shows_its_views_and_follows_its_state()
    {
        var probe = new ViewMethodProbe();
        using var t = new ReactorTestHost(probe);
        Assert.False(t.HasText("Beaucoup"));

        probe.Progress = 2;
        t.Settle();

        Assert.True(t.HasText("Observé 2"));
        Assert.True(t.HasText("Beaucoup"));
    }
}
