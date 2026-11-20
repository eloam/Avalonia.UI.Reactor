namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Parent qui affiche toujours la même instance d'enfant : l'enfant garde son état quand le parent est réévalué.</summary>
public sealed partial class ParentProbe : Component
{
    readonly ChildProbe _child = new();

    [State] partial int Clicks { get; set; }

    public override View Body => VStack(Button($"parent {Clicks}", () => Clicks++), _child);
}
