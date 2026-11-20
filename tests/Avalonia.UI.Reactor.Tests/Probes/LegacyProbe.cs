using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

public class LegacyProbe : Component
{
    int _value;
    public void Set(int v) { _value = v; Refresh(); }
    public override View Body => Text($"{_value}");
}
