// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A counter with modifiers: it is wrapped, but keeps its state and its own re-evaluation.
/// </summary>
public sealed class PaddedCounterProbe : Component
{
    public CounterProbe Counter { get; } = new();

    public override View Body => VStack(Counter.Padding(8));
}
