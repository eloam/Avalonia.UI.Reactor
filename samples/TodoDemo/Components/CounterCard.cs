// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace TodoDemo.Components;

/// <summary>
/// A self-contained component: clicking "+" re-evaluates this card only.
/// </summary>
public sealed partial class CounterCard : Component
{
    [State] private partial int Count { get; set; }

    public override View Body =>
        Card(
            Text("Counter").Font(TextStyle.Headline),
            HStack(
                Button("−", () => Count--).Style(ButtonStyle.Secondary),
                Text($"{Count}").FontSize(28).Bold().Width(72).TextAlignment(TextAlign.Center),
                Button("+", () => Count++),
                Spacer(),
                Count != 0 ? Button("Reset", () => Count = 0).Style(ButtonStyle.Ghost) : null));
}
