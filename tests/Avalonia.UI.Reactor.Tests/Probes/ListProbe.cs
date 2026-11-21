// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A list whose rows are identified by their text; a button reverses the order.
/// </summary>
public sealed class ListProbe : Component
{
    public StateList<string> Items { get; } = ["a", "b", "c"];

    public override View Body =>
        VStack(
            ForEach(Items, Text, item => item),
            Button("Inverser", () =>
            {
                var reversed = Items.Reverse().ToList();
                Items.Clear();
                Items.AddRange(reversed);
            }));
}
