namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Enfant de <see cref="ParentProbe"/>, avec son propre état.</summary>
public sealed partial class ChildProbe : Component
{
    [State] partial int Clicks { get; set; }

    public override View Body => Button($"enfant {Clicks}", () => Clicks++);
}
