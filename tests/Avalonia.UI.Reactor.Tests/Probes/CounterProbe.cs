// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A counter: a state, its text and buttons that change it; <paramref name="name"/> tells two counters apart.
/// </summary>
public sealed partial class CounterProbe(string name = "") : Component
{
    [State] public partial int Count { get; set; }

    public override View Body =>
        HStack(
            Button($"{name}−", () => Count--),
            Text($"{name}{Count}"),
            Button($"{name}+", () => Count++),
            Count != 0 ? Button($"{name}Remettre à zéro", () => Count = 0) : null);
}
