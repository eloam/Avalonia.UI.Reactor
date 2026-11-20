using Mvu.Tests.Probes;

namespace Mvu.Tests;

/// <summary>Actions async Task et erreurs non gérées : signalées, jamais fatales.</summary>
public class AsyncAndErrorTests
{
    [Fact]
    public void Async_Task_button_updates_before_and_after_await()
    {
        var probe = new AsyncProbe();
        using var t = new TestHost(probe);

        t.ClickButton("Charger");
        Assert.True(t.HasText("chargement…"));

        t.WaitUntil(() => t.HasText("chargé"));
    }

    [Fact]
    public void Exception_in_an_action_is_reported_and_the_app_keeps_working()
    {
        var probe = new FailingProbe();
        using var t = new TestHost(probe);
        var errors = new List<Exception>();
        t.Ui.UnhandledException += errors.Add;

        t.ClickButton("Planter");
        Assert.Single(errors);
        Assert.Equal("boom", errors[0].Message);

        t.ClickButton("+ 0");
        Assert.True(t.HasButton("+ 1"));
    }

    [Fact]
    public void Exception_after_await_in_an_async_Task_is_reported()
    {
        var probe = new FailingProbe();
        using var t = new TestHost(probe);
        var errors = new List<Exception>();
        t.Ui.UnhandledException += errors.Add;

        t.ClickButton("Planter plus tard");
        t.WaitUntil(() => errors.Count == 1);
        Assert.Equal("boom async", errors[0].Message);
    }

    [Fact]
    public void Exception_in_an_async_void_is_reported_too()
    {
        var probe = new FailingProbe();
        using var t = new TestHost(probe);
        var errors = new List<Exception>();
        t.Ui.UnhandledException += errors.Add;

        t.ClickButton("async void");
        t.WaitUntil(() => errors.Count == 1);
        Assert.Equal("boom void", errors[0].Message);
    }
}
