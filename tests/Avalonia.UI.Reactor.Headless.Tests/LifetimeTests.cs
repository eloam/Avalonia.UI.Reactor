using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Headless.Tests.Probes;

/// <summary>Une fenêtre fermée ne reste attachée ni aux états ni aux composants qu'elle affichait.</summary>
public class LifetimeTests
{
    [AvaloniaFact]
    public void Closing_the_window_detaches_the_components_and_keeps_their_state()
    {
        var probe = new CounterProbe();
        var t = new ReactorTestHost(probe);
        t.ClickButton("+");
        Assert.NotNull(probe.Host);

        t.Dispose();
        probe.Count++; // un état modifié après la fermeture ne réveille plus la fenêtre

        Assert.Null(probe.Host);
        Assert.Equal(2, probe.Count);
    }
}
