// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A task list: each checkbox is bound to the state of its task (<c>Checkbox(task.Bind.Done, task.Title)</c>).
/// </summary>
public sealed class TaskListProbe : Component
{
    public StateList<TaskItem> Tasks { get; } = [new("Écrire les tests"), new("Relire")];

    public override View Body => VStack(ForEach(Tasks, task => Checkbox(task.Bind.Done, task.Title)));
}
