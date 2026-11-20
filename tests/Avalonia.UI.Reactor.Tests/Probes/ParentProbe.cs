using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

public partial class ParentProbe : Component
{
    [State] partial int Clicks { get; set; }
    readonly ChildProbe child = new();
    public override View Body => VStack(Button($"parent {Clicks}", () => Clicks++), child);
}
