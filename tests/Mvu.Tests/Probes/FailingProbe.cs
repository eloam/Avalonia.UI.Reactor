using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

public partial class FailingProbe : Component
{
    [State] partial int Count { get; set; }

    public override View Body => VStack(
        Button("Planter", () => throw new InvalidOperationException("boom")),
        Button("Planter plus tard", async () =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("boom async");
        }),
        Button("async void", FireAndForget),
        Button($"+ {Count}", () => Count++));

    async void FireAndForget()
    {
        await Task.Delay(10);
        throw new InvalidOperationException("boom void");
    }
}
