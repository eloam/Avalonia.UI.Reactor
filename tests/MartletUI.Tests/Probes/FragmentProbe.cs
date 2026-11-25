// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A block of several views in a row, between two texts, with a branch that opens in it.
/// </summary>
public sealed partial class FragmentProbe : Component
{
    [State] public partial bool Show { get; set; }

    public override View Body => HStack(
        Text("avant"),
        Observe(() => Group(Text("a"), Show ? Text("x") : null, Text("b"))),
        Text("après"));
}
