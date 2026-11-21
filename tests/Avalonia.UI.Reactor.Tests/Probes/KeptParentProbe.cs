// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A parent re-evaluated on its own, around a child component whose state does not change.
/// </summary>
public sealed partial class KeptParentProbe : Component
{
    [State] public partial int Count { get; set; }

    public KeptChildProbe Child { get; } = new();

    public override View Body => VStack(Text($"Compte {Count}"), Child);
}
