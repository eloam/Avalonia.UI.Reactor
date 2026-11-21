// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// Two components of the same type, the first one optional: each is identified by its instance.
/// </summary>
public sealed partial class OptionalCounterProbe : Component
{
    [State] public partial bool ShowFirst { get; set; }

    public CounterProbe First { get; } = new("a");

    public CounterProbe Second { get; } = new("b");

    public override View Body => VStack(ShowFirst ? First : null, Second);
}
