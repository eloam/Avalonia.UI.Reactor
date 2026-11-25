// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A child component with a lifecycle, kept as it is while its parent is re-evaluated.
/// </summary>
public sealed class KeptChildProbe : Component
{
    public int Appeared { get; private set; }

    public int Disappeared { get; private set; }

    public override View Body => Text("Enfant").OnAppear(() => Appeared++).OnDisappear(() => Disappeared++);
}
