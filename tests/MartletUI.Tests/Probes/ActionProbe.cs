// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// Async actions (the interface follows the state before and after the await) and failing actions.
/// </summary>
public sealed partial class ActionProbe : Component
{
    [State] public partial bool Thinking { get; set; }

    [State] public partial int Clicks { get; set; }

    /// <summary>
    /// Gets the gate that holds the async action at its await until the test releases it.
    /// </summary>
    public TaskCompletionSource Gate { get; } = new();

    public override View Body =>
        VStack(
            Button(Thinking ? "…" : "Idée", SuggestAsync),
            Button("Échouer", Fail),
            Button("Échouer plus tard", FailLaterAsync),
            Button("Échouer sans tâche", FailWithoutTask),
            Button($"Clics : {Clicks}", () => Clicks++));

    private async Task SuggestAsync()
    {
        Thinking = true;
        await Gate.Task;
        Thinking = false;
    }

    private static void Fail() => throw new InvalidOperationException("Sync failure.");

    private static async Task FailLaterAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("Async failure.");
    }

    // Written as async void on purpose: the error goes through no task, and the host receives it from the dispatcher.
    private static async void FailWithoutTask()
    {
        await Task.Yield();
        throw new InvalidOperationException("Async void failure.");
    }
}
