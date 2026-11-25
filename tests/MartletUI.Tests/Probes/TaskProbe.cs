// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Threading;
using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A view that runs a task, with or without a key, which can fail, be removed from the view, or disappear with it.
/// </summary>
/// <param name="useKey">A value indicating whether the task is keyed by <see cref="Query"/>.</param>
/// <param name="fail">A value indicating whether the task throws after its first await.</param>
public sealed partial class TaskProbe(bool useKey = false, bool fail = false) : Component
{
    [State] public partial bool Shown { get; set; } = true;

    [State] public partial bool WithTask { get; set; } = true;

    [State] public partial string Query { get; set; } = "a";

    [State] public partial int Count { get; set; }

    public int Starts { get; private set; }

    public int Cancellations { get; private set; }

    public List<bool> ResumedOnUIThread { get; } = [];

    public override View Body
    {
        get
        {
            TextView text = Text($"Compte {Count}");
            View watched = !WithTask ? text : useKey ? text.Task(Query, RunAsync) : text.Task(RunAsync);
            return VStack(Shown ? watched : null);
        }
    }

    private async Task RunAsync(CancellationToken cancellation)
    {
        Starts++;
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellation);
            ResumedOnUIThread.Add(Dispatcher.UIThread.CheckAccess());
            if (fail)
            {
                throw new InvalidOperationException("Task failure.");
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
        }
        catch (OperationCanceledException)
        {
            Cancellations++;
            throw;
        }
    }
}
