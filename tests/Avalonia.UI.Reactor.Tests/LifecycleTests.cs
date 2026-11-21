// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Reconciliation;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// <c>OnAppear</c> and <c>OnDisappear</c> follow the node of a view, not its control, and run after the update.
/// </summary>
public class LifecycleTests
{
    [AvaloniaFact]
    public void OnAppear_runs_once_and_not_again_on_an_update()
    {
        var probe = new AppearProbe();
        using var t = new ReactorTestHost(probe);
        t.Settle();
        Assert.Equal(1, probe.Appeared);

        probe.Count++;
        t.Settle();

        Assert.True(t.HasText("Compte 1"));
        Assert.Equal(1, probe.Appeared);
        Assert.Equal(0, probe.Disappeared);
    }

    [AvaloniaFact]
    public void OnDisappear_runs_when_a_branch_closes_and_OnAppear_when_it_opens_again()
    {
        var probe = new AppearProbe();
        using var t = new ReactorTestHost(probe);
        t.Settle();

        probe.Shown = false;
        t.Settle();
        Assert.Equal(1, probe.Disappeared);

        probe.Shown = true;
        t.Settle();
        Assert.Equal(2, probe.Appeared);
    }

    [AvaloniaFact]
    public void Nothing_runs_when_the_control_moves_into_a_decoration_and_out_of_it()
    {
        var probe = new AppearProbe();
        using var t = new ReactorTestHost(probe);
        t.Settle();

        probe.Highlighted = true;
        t.Settle();
        Assert.Equal("Surface", t.TextShowing("Observée").Parent?.GetType().Name);
        probe.Highlighted = false;
        t.Settle();

        Assert.Equal(1, probe.Appeared);
        Assert.Equal(0, probe.Disappeared);
    }

    [AvaloniaFact]
    public void A_removed_row_disappears_and_reordered_rows_do_not()
    {
        var probe = new LifecycleRowsProbe();
        using var t = new ReactorTestHost(probe);
        t.Settle();
        Assert.Equal(["a", "b", "c"], probe.Appeared);

        probe.Items.Move(0, 2);
        t.Settle();
        Assert.Equal(3, probe.Appeared.Count);
        Assert.Empty(probe.Disappeared);

        probe.Items.Remove("b");
        t.Settle();
        Assert.Equal(["b"], probe.Disappeared);
    }

    [AvaloniaFact]
    public void OnDisappear_runs_at_once_when_the_window_closes()
    {
        var probe = new AppearProbe();
        var t = new ReactorTestHost(probe);
        t.Settle();

        t.Dispose();

        Assert.Equal(1, probe.Disappeared);
    }

    [AvaloniaFact]
    public void A_lifecycle_in_a_scope_kept_by_an_update_stays_alive()
    {
        var probe = new KeptParentProbe();
        using var t = new ReactorTestHost(probe);
        t.Settle();

        probe.Count++;
        t.Settle();

        Assert.True(t.Stats.Reused > 0);
        Assert.Equal(1, probe.Child.Appeared);
        Assert.Equal(0, probe.Child.Disappeared);
    }

    [AvaloniaFact]
    public void A_component_appears_and_disappears_with_the_modifiers_of_its_parent()
    {
        var probe = new ComponentLifecycleProbe();
        using var t = new ReactorTestHost(probe);
        t.Settle();
        Assert.Equal(1, probe.Appeared);

        probe.Shown = false;
        t.Settle();

        Assert.Equal(1, probe.Disappeared);
    }

    [AvaloniaFact]
    public void Parents_appear_before_their_children_and_disappear_after_them()
    {
        var log = new List<string>();
        var reconciler = new Reconciler(new StubHost(), action => action?.Invoke());
        View tree = VStack(Text("child").OnAppear(() => log.Add("child appears")).OnDisappear(() => log.Add("child disappears")))
            .OnAppear(() => log.Add("parent appears"))
            .OnDisappear(() => log.Add("parent disappears"));

        reconciler.Update(tree, Themes.Default);
        Dispatcher.UIThread.RunJobs();
        reconciler.Update(Text("other"), Themes.Default);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["parent appears", "child appears", "child disappears", "parent disappears"], log);
    }

    [AvaloniaFact]
    public void A_view_that_leaves_before_its_appearance_runs_gets_neither_action()
    {
        int appeared = 0;
        int disappeared = 0;
        var reconciler = new Reconciler(new StubHost(), action => action?.Invoke());

        reconciler.Update(VStack(Text("brief").OnAppear(() => appeared++).OnDisappear(() => disappeared++)), Themes.Default);
        reconciler.Update(VStack(), Themes.Default);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, appeared);
        Assert.Equal(0, disappeared);
    }

    [AvaloniaFact]
    public void An_exception_in_OnAppear_is_reported()
    {
        using var t = new ReactorTestHost(new ViewProbe(() =>
            Text("x").OnAppear(() => throw new InvalidOperationException("Appear failure."))));

        t.Settle();

        Assert.IsType<InvalidOperationException>(Assert.Single(t.Errors));
    }

    private sealed class StubHost : IScopeHost
    {
        public Themes Themes { get; } = Themes.Default;

        public void RequestUpdate() { }

        public void Report(Exception error) => throw error;

        public void Reload() { }
    }
}
