using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;

/// <summary>Actions async et erreurs : l'interface suit l'état autour de l'await, une erreur ne ferme pas l'application.</summary>
public class ActionTests
{
    [AvaloniaFact]
    public void An_async_action_updates_the_view_before_and_after_its_await()
    {
        var probe = new ActionProbe();
        using var t = new ReactorTestHost(probe);

        t.ClickButton("Idée");
        Assert.True(t.HasButton("…"));

        probe.Gate.SetResult();
        t.Settle();
        Assert.True(t.HasButton("Idée"));
    }

    [AvaloniaFact]
    public void An_error_in_an_action_is_reported_and_the_application_continues()
    {
        using var t = new ReactorTestHost(new ActionProbe());

        t.ClickButton("Échouer");
        t.ClickButton("Clics : 0");

        Assert.Equal("Sync failure.", Assert.Single(t.Errors).Message);
        Assert.True(t.HasButton("Clics : 1"));
    }

    [AvaloniaFact]
    public void An_error_after_an_await_is_reported_to_the_window_that_ran_the_action()
    {
        using var t = new ReactorTestHost(new ActionProbe());

        t.ClickButton("Échouer plus tard");
        t.Settle();

        Assert.Equal("Async failure.", Assert.Single(t.Errors).Message);
    }

    [AvaloniaFact]
    public void An_error_after_an_await_in_an_async_void_action_is_reported_too()
    {
        using var t = new ReactorTestHost(new ActionProbe());

        t.ClickButton("Échouer sans tâche");
        t.WaitUntil(() => t.Errors.Count > 0);
        t.ClickButton("Clics : 0");

        Assert.Equal("Async void failure.", Assert.Single(t.Errors).Message);
        Assert.True(t.HasButton("Clics : 1"));
    }
}
