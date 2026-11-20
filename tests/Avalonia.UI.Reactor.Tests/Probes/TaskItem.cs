using Avalonia.UI.Reactor.State;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>Objet modèle avec un état observé : ses liaisons sont accessibles de l'extérieur (item.Bind.Done).</summary>
public partial class TaskItem(string title)
{
    public string Title { get; } = title;
    [State] public partial bool Done { get; set; }
}
