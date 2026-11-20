using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

public partial class Probe : Component
{
    [State] public partial int N { get; set; }
    public override View Body => VStack(Text($"N = {N}"));
}
