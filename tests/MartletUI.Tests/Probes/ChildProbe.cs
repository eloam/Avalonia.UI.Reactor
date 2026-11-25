// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// The child of <see cref="ParentProbe"/>, with its own state.
/// </summary>
public sealed partial class ChildProbe : Component
{
    [State] private partial int Clicks { get; set; }

    public override View Body => Button($"enfant {Clicks}", () => Clicks++);
}
