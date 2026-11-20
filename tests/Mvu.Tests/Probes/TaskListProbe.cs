using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

/// <summary>Liste de tâches : chaque case est liée à l'état de sa tâche (Checkbox(task.Bind.Done)).</summary>
public class TaskListProbe : Component
{
    public StateList<TaskItem> Tasks { get; } = [new("Écrire les tests"), new("Relire")];

    public override View Body => VStack(ForEach(Tasks, task => Checkbox(task.Bind.Done, task.Title)));
}
