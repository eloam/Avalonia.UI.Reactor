namespace Avalonia.UI.Reactor.Headless.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Deux compteurs indépendants sous un parent sans état.</summary>
public sealed class TwoCountersProbe : Component
{
    public CounterProbe First { get; } = new("a");

    public CounterProbe Second { get; } = new("b");

    public override View Body => VStack(Text("Deux compteurs"), First, Second);
}
