// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A counter with modifiers: it is wrapped, but keeps its state and its own re-evaluation.
/// </summary>
public sealed class PaddedCounterProbe : Component
{
    public CounterProbe Counter { get; } = new();

    public override View Body => VStack(Counter.Padding(8));
}
