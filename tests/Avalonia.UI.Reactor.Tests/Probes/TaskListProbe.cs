namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Liste de tâches : chaque case est liée à l'état de sa tâche (<c>Checkbox(task.Bind.Done, task.Title)</c>).</summary>
public sealed class TaskListProbe : Component
{
    public StateList<TaskItem> Tasks { get; } = [new("Écrire les tests"), new("Relire")];

    public override View Body => VStack(ForEach(Tasks, task => Checkbox(task.Bind.Done, task.Title)));
}
