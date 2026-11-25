// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A field in a block of several views, below an error message that appears in the block.
/// </summary>
public sealed partial class FragmentFieldProbe : Component
{
    [State] public partial bool HasError { get; set; }

    [State] public partial string Email { get; set; } = "";

    public override View Body => VStack(
        Observe(() => Group(HasError ? Text("Erreur") : null, TextField(Email, value => Email = value))),
        Text("après"));
}
