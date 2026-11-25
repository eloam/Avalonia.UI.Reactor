// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Threading;
using MartletUI.State;
using MartletUI.Views;

namespace MartletUI.Reconciliation;

/// <summary>
/// Runs the lifecycles of the views: <c>OnAppear</c>, <c>OnDisappear</c> and <c>Task</c>.
/// </summary>
/// <remarks>
/// A lifecycle follows the node of its view in the reconciler, never the Avalonia control, which a decoration detaches
/// and attaches again. The actions run after the update, on the dispatcher, so that a state they change schedules the
/// next update instead of changing a <c>Body</c> being computed.
/// </remarks>
/// <param name="host">The host that reports the exceptions of the tasks.</param>
/// <param name="run">The function that runs an action, then updates the interface.</param>
internal sealed class LifecycleTracker(IScopeHost host, Action<Action?> run)
{
    private readonly HashSet<Lifecycle> _lifecycles = [];

    // The number of the last visit, which orders the lifecycles as the tree is.
    private long _visits;

    /// <summary>
    /// Records that the update visited a view that declares a lifecycle.
    /// </summary>
    /// <remarks>
    /// The reconciler calls it before it visits the children of the view, so that the order of the visits is the order
    /// of the tree.
    /// </remarks>
    /// <param name="node">The node of the view.</param>
    /// <param name="view">The view.</param>
    /// <param name="owner">The scope being reconciled, which declares the view, or <see langword="null"/>.</param>
    /// <param name="pass">The number of the update.</param>
    /// <param name="depth">The depth of the view in the tree.</param>
    public void Track(Node node, View view, Scope? owner, int pass, int depth)
    {
        if (view.AppearAction is null && view.DisappearAction is null && view.TaskAction is null)
        {
            return;
        }

        if (node.Lifecycle is not { } lifecycle)
        {
            lifecycle = new Lifecycle(node);
            node.Lifecycle = lifecycle;
            _lifecycles.Add(lifecycle);
        }

        lifecycle.Owner = owner;
        lifecycle.LastVisit = pass;
        lifecycle.Depth = depth;
        lifecycle.Order = ++_visits;
    }

    /// <summary>
    /// Decides, after an update, which views disappeared, which appeared, and which tasks restart, and posts their
    /// actions on the dispatcher.
    /// </summary>
    /// <remarks>
    /// A lifecycle is alive when its node was visited, or when the scope that declares it was kept by the update without
    /// being re-evaluated: the nodes of such a scope are not visited, and without this rule a child component would
    /// disappear whenever its parent is re-evaluated. The disappearances run first, children before their parents, then
    /// the appearances and the restarted tasks, parents before their children. A task is cancelled at once, when the
    /// update decides it.
    /// </remarks>
    /// <param name="pass">The number of the update.</param>
    /// <param name="isKept">The function that tells whether the update kept a scope without re-evaluating it.</param>
    public void Sweep(int pass, Func<Scope?, bool> isKept)
    {
        List<Lifecycle>? gone = null;
        List<Lifecycle>? appearing = null;
        List<Lifecycle>? restarting = null;
        foreach (Lifecycle lifecycle in _lifecycles)
        {
            if (lifecycle.LastVisit != pass && !isKept(lifecycle.Owner))
            {
                (gone ??= []).Add(lifecycle);
            }
            else if (!lifecycle.Appeared)
            {
                lifecycle.Appeared = true;
                DecideTask(lifecycle);
                (appearing ??= []).Add(lifecycle);
            }
            else if (TaskChanged(lifecycle))
            {
                lifecycle.Cancellation?.Cancel();
                DecideTask(lifecycle);
                if (lifecycle.TaskStarted)
                    (restarting ??= []).Add(lifecycle);
            }
        }

        if (gone is null && appearing is null && restarting is null)
        {
            return;
        }

        gone?.ForEach(Forget);
        gone?.Sort(Lifecycle.ChildrenFirst);
        appearing?.Sort(Lifecycle.ParentsFirst);
        restarting?.Sort(Lifecycle.ParentsFirst);
        Dispatcher.UIThread.Post(() =>
        {
            gone?.ForEach(Disappear);
            appearing?.ForEach(Appear);
            restarting?.ForEach(StartTask);
        });
    }

    /// <summary>
    /// Ends every lifecycle when the window closes: the dispatcher may not run again, so the views that appeared
    /// disappear at once, and their tasks are cancelled.
    /// </summary>
    public void Release()
    {
        List<Lifecycle> all = [.. _lifecycles];
        all.Sort(Lifecycle.ChildrenFirst);
        all.ForEach(Forget);
        all.ForEach(Disappear);
    }

    // The task was added to a view that had appeared, removed from it, or its key changed.
    private static bool TaskChanged(Lifecycle lifecycle)
    {
        View view = lifecycle.Node.View;
        return view.TaskAction is null
            ? lifecycle.TaskStarted
            : !lifecycle.TaskStarted || !Equals(lifecycle.TaskKey, view.TaskKey);
    }

    private static void DecideTask(Lifecycle lifecycle)
    {
        lifecycle.TaskStarted = lifecycle.Node.View.TaskAction is not null;
        lifecycle.TaskKey = lifecycle.Node.View.TaskKey;
    }

    private void Forget(Lifecycle lifecycle)
    {
        _lifecycles.Remove(lifecycle);
        lifecycle.Gone = true;
        lifecycle.Cancellation?.Cancel();
        if (ReferenceEquals(lifecycle.Node.Lifecycle, lifecycle))
        {
            lifecycle.Node.Lifecycle = null;
        }
    }

    // A view that went away before its posted appearance ran gets neither OnAppear nor OnDisappear, nor its task.
    private void Appear(Lifecycle lifecycle)
    {
        if (lifecycle.Gone)
        {
            return;
        }

        lifecycle.AppearRan = true;
        run(lifecycle.Node.View.AppearAction);
        StartTask(lifecycle);
    }

    // Started on the user interface thread, after the update: after an await, the task resumes there. A run that is
    // still going (two restarts decided before the first one started) is cancelled first, so that one run at most goes on.
    private void StartTask(Lifecycle lifecycle)
    {
        if (lifecycle.Gone || !lifecycle.TaskStarted || lifecycle.Node.View.TaskAction is not { } task)
        {
            return;
        }

        lifecycle.Cancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        lifecycle.Cancellation = cancellation;
        run(() => _ = WatchAsync(lifecycle, task, cancellation));
    }

    // Runs the task until it ends. An OperationCanceledException after cancellation is expected; another exception is
    // reported as the exception of an action is. The cancellation is disposed when the task ends.
    private async Task WatchAsync(Lifecycle lifecycle, Func<CancellationToken, Task> task, CancellationTokenSource cancellation)
    {
        try
        {
            await task(cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Expected: the view disappeared, the key changed, or the window closed.
        }
        catch (Exception exception)
        {
            host.Report(exception);
        }
        finally
        {
            if (ReferenceEquals(lifecycle.Cancellation, cancellation))
            {
                lifecycle.Cancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private void Disappear(Lifecycle lifecycle)
    {
        if (lifecycle.AppearRan)
        {
            run(lifecycle.Node.View.DisappearAction);
        }
    }
}
