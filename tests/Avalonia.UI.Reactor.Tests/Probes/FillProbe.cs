using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

public partial class FillProbe : Component
{
    [State] partial int Clicks { get; set; }
    public override View Body => Button($"{Clicks}", () => Clicks++).FillWidth();
}
