// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.State;

/// <summary>
/// Tracks the host that runs the current action on this thread (a click, a keystroke).
/// </summary>
/// <remarks>
/// An async action captures the host before its first <see langword="await"/>, so that it can report its errors to that
/// host whatever context its continuation runs on.
/// </remarks>
internal static class RunningAction
{
    /// <summary>
    /// Gets the host of the action running on this thread, or <see langword="null"/> outside an action.
    /// </summary>
    [field: ThreadStatic]
    public static IScopeHost? Host { get; private set; }

    /// <summary>
    /// Runs an action of the user interface on behalf of a host.
    /// </summary>
    /// <param name="host">The host that runs the action.</param>
    /// <param name="action">The action to run.</param>
    public static void Run(IScopeHost host, Action action)
    {
        IScopeHost? previous = Host;
        Host = host;
        try
        {
            action();
        }
        finally
        {
            Host = previous;
        }
    }
}
