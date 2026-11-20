using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>Composant dont le Body est fourni par le test.</summary>
public class ViewProbe(Func<View> body) : Component
{
    public override View Body => body();
}
