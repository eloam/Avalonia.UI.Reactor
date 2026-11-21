// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.Controls;
using Avalonia.UI.Reactor.Panels;
using Avalonia.UI.Reactor.Reconciliation;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// The reconciler reuses controls from one update to the next and re-evaluates only the affected scopes.
/// </summary>
public class ReconcilerTests
{
    [AvaloniaFact]
    public void Clicking_a_button_runs_its_action_and_updates_the_same_text_control()
    {
        using var t = new ReactorTestHost(new CounterProbe());
        TextBlock text = t.TextShowing("0");

        t.ClickButton("+");

        Assert.Same(text, t.TextShowing("1"));
    }

    [AvaloniaFact]
    public void A_view_that_appears_and_disappears_is_added_then_removed()
    {
        using var t = new ReactorTestHost(new CounterProbe());
        Assert.False(t.HasButton("Remettre à zéro"));

        t.ClickButton("+");
        Assert.True(t.HasButton("Remettre à zéro"));

        t.ClickButton("Remettre à zéro");
        Assert.False(t.HasButton("Remettre à zéro"));
        Assert.True(t.HasText("0"));
    }

    [AvaloniaFact]
    public void Only_the_component_whose_state_changed_is_reevaluated()
    {
        var probe = new TwoCountersProbe();
        using var t = new ReactorTestHost(probe);

        t.ClickButton("a+");

        Assert.Equal(new UpdateStats(Evaluated: 1, Reused: 1, Scopes: 3), t.Stats);
        Assert.True(t.HasText("a1"));
        Assert.True(t.HasText("b0"));
    }

    [AvaloniaFact]
    public void Reordering_a_keyed_list_moves_its_controls_instead_of_recreating_them()
    {
        using var t = new ReactorTestHost(new ListProbe());
        (TextBlock? a, TextBlock? b, TextBlock? c) = (t.TextShowing("a"), t.TextShowing("b"), t.TextShowing("c"));

        t.ClickButton("Inverser");

        Assert.Equal([c, b, a], t.Texts);
    }

    [AvaloniaFact]
    public void A_decoration_declared_later_wraps_the_same_text_control()
    {
        using var t = new ReactorTestHost(new HighlightProbe());
        TextBlock text = t.TextShowing("mot");
        Assert.IsType<FlexStack>(text.Parent);

        t.ClickButton("Surligner");
        Assert.Same(text, t.TextShowing("mot"));
        Assert.IsType<Surface>(text.Parent);

        t.ClickButton("Surligner");
        Assert.Same(text, t.TextShowing("mot"));
        Assert.IsType<FlexStack>(text.Parent);
    }

    [AvaloniaFact]
    public void Two_siblings_with_the_same_key_are_reported()
    {
        var reconciler = new Reconciler(new StubHost(), _ => { });

        InvalidOperationException e = Assert.Throws<InvalidOperationException>(() =>
            reconciler.Update(VStack(Text("a").Key("k"), Text("b").Key("k")), Themes.Default));

        Assert.Equal("Two children of the same container have the key 'k'. Give each child a unique key.", e.Message);
    }

    private sealed class StubHost : IScopeHost
    {
        public Themes Themes { get; } = Themes.Default;

        public void RequestUpdate() { }

        public void Report(Exception error) => throw error;

        public void Reload() { }
    }
}
