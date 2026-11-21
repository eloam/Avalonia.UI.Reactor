// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// The child of <see cref="ParentProbe"/>, with its own state.
/// </summary>
public sealed partial class ChildProbe : Component
{
    [State] private partial int Clicks { get; set; }

    public override View Body => Button($"enfant {Clicks}", () => Clicks++);
}
