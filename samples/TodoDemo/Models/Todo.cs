// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.State;

namespace TodoDemo.Models;

/// <summary>
/// An observed model: checking a task re-evaluates only what displays that task. Its identifier is the key of its row
/// in a list (<c>ForEach(todos, Row)</c>).
/// </summary>
public sealed partial class Todo(string title, bool done = false) : IIdentifiable<int>
{
    private static int s_nextId;
    public int Id { get; } = ++s_nextId;
    [State] public partial string Title { get; set; } = title;
    [State] public partial bool Done { get; set; } = done;
}
