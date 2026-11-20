using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

public class LegacyProbe : Component
{
    int _value;
    public void Set(int v) { _value = v; Refresh(); }
    public override View Body => Text($"{_value}");
}
