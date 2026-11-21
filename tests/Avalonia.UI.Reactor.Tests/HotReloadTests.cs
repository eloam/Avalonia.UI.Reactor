// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Tests.Probes;

/// <summary>
/// .NET hot reload re-evaluates every scope of an Avalonia window without losing the state.
/// </summary>
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
