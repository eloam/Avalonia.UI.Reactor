using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Headless.Tests.Probes;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.State;

/// <summary>Le hot reload de .NET réévalue toutes les portées d'une fenêtre Avalonia, sans perdre l'état.</summary>
public class HotReloadTests
{
    [AvaloniaFact]
    public void Hot_reload_reevaluates_every_component_and_keeps_their_state()
    {
        using var t = new ReactorTestHost(new TwoCountersProbe());
        t.ClickButton("a+");

        HotReload.UpdateApplication(null);
        t.Settle();

        Assert.Equal(new UpdateStats(Evaluated: 3, Reused: 0, Scopes: 3), t.Stats);
        Assert.True(t.HasText("a1"));
    }
}
