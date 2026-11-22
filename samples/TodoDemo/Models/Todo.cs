// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace TodoDemo.Models;

/// <summary>
/// Represents a task: an observed model, so that checking it re-evaluates only what displays it.
/// </summary>
/// <remarks>
/// Its identifier is the key of its row in a list (<c>ForEach(todos, Row)</c>), as with <c>Identifiable</c> in SwiftUI:
/// a row keeps its controls, focus and hover when the list changes order.
/// </remarks>
/// <param name="title">The title of the task.</param>
/// <param name="done"><see langword="true"/> if the task is already done; otherwise, <see langword="false"/>.</param>
public sealed partial class Todo(string title, bool done = false) : IIdentifiable<int>
{
    private static int s_lastId;

    /// <summary>
    /// Gets the identifier of the task, unique in the application.
    /// </summary>
    public int Id { get; } = Interlocked.Increment(ref s_lastId);

    /// <summary>
    /// Gets or sets the title of the task.
    /// </summary>
    [State]
    public partial string Title { get; set; } = title;

    /// <summary>
    /// Gets or sets a value indicating whether the task is done.
    /// </summary>
    [State]
    public partial bool Done { get; set; } = done;
}
