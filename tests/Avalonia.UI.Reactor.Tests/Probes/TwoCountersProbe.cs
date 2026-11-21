// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// Two independent counters under a parent without state.
/// </summary>
public sealed class TwoCountersProbe : Component
{
    public CounterProbe First { get; } = new("a");

    public CounterProbe Second { get; } = new("b");

    public override View Body => VStack(Text("Deux compteurs"), First, Second);
}
