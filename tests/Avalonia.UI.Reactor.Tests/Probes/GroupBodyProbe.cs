// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A component whose Body is a group of two views.
/// </summary>
public sealed class GroupBodyProbe : Component
{
    public override View Body => Group(Text("c1"), Text("c2"));
}
