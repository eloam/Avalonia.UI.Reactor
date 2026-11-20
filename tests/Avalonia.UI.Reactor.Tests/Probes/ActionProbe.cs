namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Actions async (l'interface suit l'état avant et après l'await) et actions en erreur.</summary>
public sealed partial class ActionProbe : Component
{
    [State] public partial bool Thinking { get; set; }

    [State] public partial int Clicks { get; set; }

    /// <summary>Retient l'action async à son await, jusqu'à ce que le test la libère.</summary>
    public TaskCompletionSource Gate { get; } = new();

    public override View Body =>
        VStack(
            Button(Thinking ? "…" : "Idée", Suggest),
            Button("Échouer", Fail),
            Button("Échouer plus tard", FailLater),
            Button("Échouer sans tâche", FailWithoutTask),
            Button($"Clics : {Clicks}", () => Clicks++));

    async Task Suggest()
    {
        Thinking = true;
        await Gate.Task;
        Thinking = false;
    }

    static void Fail() => throw new InvalidOperationException("Sync failure.");

    static async Task FailLater()
    {
        await Task.Yield();
        throw new InvalidOperationException("Async failure.");
    }

    // Écrit exprès en async void : l'erreur ne remonte par aucune tâche, l'hôte la reçoit du répartiteur.
    static async void FailWithoutTask()
    {
        await Task.Yield();
        throw new InvalidOperationException("Async void failure.");
    }
}
