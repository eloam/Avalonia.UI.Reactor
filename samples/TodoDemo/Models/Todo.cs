using Avalonia.UI.Reactor.State;

namespace TodoDemo;

/// <summary>Modèle observé : cocher une tâche ne recalcule que ce qui affiche cette tâche.</summary>
public sealed partial class Todo(string title, bool done = false)
{
    static int _nextId;
    public int Id { get; } = ++_nextId;
    [State] public partial string Title { get; set; } = title;
    [State] public partial bool Done { get; set; } = done;
}
