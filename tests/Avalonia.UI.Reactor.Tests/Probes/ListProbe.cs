using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

public partial class ListProbe : Component
{
    public StateList<string> Items { get; } = ["a"];
    public override View Body => VStack(ForEach(Items, s => Text(s)));
}
