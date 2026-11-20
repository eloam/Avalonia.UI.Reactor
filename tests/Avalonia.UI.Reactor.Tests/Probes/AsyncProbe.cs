using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests.Probes;

public partial class AsyncProbe : Component
{
    [State] partial string Status { get; set; } = "prêt";

    public override View Body => VStack(Text(Status), Button("Charger", Load));

    async Task Load()
    {
        Status = "chargement…";
        await Task.Delay(20);
        Status = "chargé";
    }
}
