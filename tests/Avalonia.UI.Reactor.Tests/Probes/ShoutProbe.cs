// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A field whose state transforms the input (to uppercase): the field shows the value of the state.
/// </summary>
public sealed partial class ShoutProbe : Component
{
    [State] public partial string Shout { get; set; } = "";

    public override View Body => VStack(TextField(Shout, value => Shout = value.ToUpperInvariant()));
}
