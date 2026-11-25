// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A component given a task by its parent, in a branch that closes.
/// </summary>
public sealed partial class ComponentTaskProbe : Component
{
    [State] public partial bool Shown { get; set; } = true;

    public CounterProbe Counter { get; } = new("c");

    public int Starts { get; private set; }

    public int Cancellations { get; private set; }

    public override View Body => VStack(Shown ? Counter.Task(RunAsync) : null);

    private async Task RunAsync(CancellationToken cancellation)
    {
        Starts++;
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
        }
        catch (OperationCanceledException)
        {
            Cancellations++;
            throw;
        }
    }
}
