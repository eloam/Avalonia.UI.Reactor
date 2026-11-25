// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using MartletUI.State;

namespace MartletUI.Widgets;

/// <summary>
/// Represents the holder of the control of a <c>Native(…)</c> view.
/// </summary>
/// <remarks>
/// The update of the control has its own scope: a state it reads makes it run again when that state changes, without
/// re-evaluating the view that contains the control, as for the drawing of a <c>Canvas</c>.
/// </remarks>
internal sealed class NativeHost : Decorator
{
    // The name of the scope of the update, which has no place in the tree of scopes.
    private const string UpdateScopeId = "native";

    private readonly Scope _updates;
    private Action? _update;
    private bool _released;
    private int _rerunPending;

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeHost"/> class.
    /// </summary>
    public NativeHost() => _updates = new Scope(RequestRerun, UpdateScopeId, identity: null) { IsReadOnly = false };

    /// <summary>
    /// Applies the update of the current view to the control, while tracking the state it reads.
    /// </summary>
    /// <param name="update">The update of the control.</param>
    public void Apply(Action update)
    {
        _update = update;
        Run(update);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // A control that gains or loses a decoration moves into or out of a surface: it is detached, then attached
        // again, without a new update. The subscriptions released on detach are taken again, from the current state.
        if (_released && _update is { } update)
        {
            Run(update);
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _updates.Release();
        _released = true;
    }

    private void Run(Action update)
    {
        _released = false;
        _updates.Evaluate(update);
    }

    // A state read by the update changed, on any thread: run the update again, once, on the user interface thread.
    private void RequestRerun()
    {
        if (Interlocked.Exchange(ref _rerunPending, 1) == 1)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            Interlocked.Exchange(ref _rerunPending, 0);

            // Detached in the meantime (the window closed): running the update would subscribe the host again, and
            // the state would keep it in memory.
            if (!_released && _update is { } update)
            {
                _updates.Evaluate(update);
            }
        });
    }
}
