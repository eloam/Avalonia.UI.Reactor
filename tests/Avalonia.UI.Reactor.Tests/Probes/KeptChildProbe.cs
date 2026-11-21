// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A child component with a lifecycle, kept as it is while its parent is re-evaluated.
/// </summary>
public sealed class KeptChildProbe : Component
{
    public int Appeared { get; private set; }

    public int Disappeared { get; private set; }

    public override View Body => Text("Enfant").OnAppear(() => Appeared++).OnDisappear(() => Disappeared++);
}
