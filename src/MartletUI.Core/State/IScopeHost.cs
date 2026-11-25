// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Theming;

namespace MartletUI.State;

/// <summary>
/// Represents what displays scopes, typically a window: it receives their update requests, provides the theme catalog
/// to the components it displays, and reports the errors of their actions.
/// </summary>
internal interface IScopeHost
{
    /// <summary>
    /// Gets the theme catalog of the application, exposed to components as <c>Component.Themes</c>.
    /// </summary>
    public Themes Themes { get; }

    /// <summary>
    /// Schedules an update because some state read by a scope changed. This method can be called from any thread.
    /// </summary>
    public void RequestUpdate();

    /// <summary>
    /// Reports an error thrown by an action, synchronous or async; the application keeps running.
    /// </summary>
    /// <param name="error">The exception thrown by the action.</param>
    public void Report(Exception error);

    /// <summary>
    /// Re-evaluates everything on the user interface thread of the host, after a hot reload. This method can be called
    /// from any thread.
    /// </summary>
    public void Reload();
}
