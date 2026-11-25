// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A parent that always displays the same child instance: the child keeps its state when the parent is re-evaluated.
/// </summary>
public sealed partial class ParentProbe : Component
{
    private readonly ChildProbe _child = new();

    [State] private partial int Clicks { get; set; }

    public override View Body => VStack(Button($"parent {Clicks}", () => Clicks++), _child);
}
