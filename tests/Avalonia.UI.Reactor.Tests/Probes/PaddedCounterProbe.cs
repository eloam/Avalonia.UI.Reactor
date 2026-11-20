namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Un compteur qui porte des modificateurs : il est enveloppé, mais garde son état et son recalcul à part.</summary>
public sealed class PaddedCounterProbe : Component
{
    public CounterProbe Counter { get; } = new();

    public override View Body => VStack(Counter.Padding(8));
}
