namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;

/// <summary>Objet modèle avec un état observé : ses liaisons sont accessibles de l'extérieur (<c>item.Bind.Done</c>).</summary>
public sealed partial class TaskItem(string title)
{
    public string Title { get; } = title;

    [State] public partial bool Done { get; set; }
}
