// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

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
