// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A parent re-evaluated on its own, around a child component whose state does not change.
/// </summary>
public sealed partial class KeptParentProbe : Component
{
    [State] public partial int Count { get; set; }

    public KeptChildProbe Child { get; } = new();

    public override View Body => VStack(Text($"Compte {Count}"), Child);
}
