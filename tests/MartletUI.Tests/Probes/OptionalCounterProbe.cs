// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

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
