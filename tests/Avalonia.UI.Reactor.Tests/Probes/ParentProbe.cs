// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A parent that always displays the same child instance: the child keeps its state when the parent is re-evaluated.
/// </summary>
public sealed partial class ParentProbe : Component
{
    private readonly ChildProbe _child = new();

    [State] private partial int Clicks { get; set; }

    public override View Body => VStack(Button($"parent {Clicks}", () => Clicks++), _child);
}
