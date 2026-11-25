// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// Two independent counters under a parent without state.
/// </summary>
public sealed class TwoCountersProbe : Component
{
    public CounterProbe First { get; } = new("a");

    public CounterProbe Second { get; } = new("b");

    public override View Body => VStack(Text("Deux compteurs"), First, Second);
}
