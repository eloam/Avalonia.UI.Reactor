// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A component given OnAppear and OnDisappear by its parent, in a branch that closes.
/// </summary>
public sealed partial class ComponentLifecycleProbe : Component
{
    [State] public partial bool Shown { get; set; } = true;

    public CounterProbe Counter { get; } = new("c");

    public int Appeared { get; private set; }

    public int Disappeared { get; private set; }

    public override View Body =>
        VStack(Shown ? Counter.OnAppear(() => Appeared++).OnDisappear(() => Disappeared++) : null);
}
