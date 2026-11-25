// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A field whose state transforms the input (to uppercase): the field shows the value of the state.
/// </summary>
public sealed partial class ShoutProbe : Component
{
    [State] public partial string Shout { get; set; } = "";

    public override View Body => VStack(TextField(Shout, value => Shout = value.ToUpperInvariant()));
}
