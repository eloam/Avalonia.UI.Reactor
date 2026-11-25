// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A field without a binding, below an error message that appears: identified by its type and its rank.
/// </summary>
public sealed partial class ErrorFieldProbe : Component
{
    [State] public partial bool HasError { get; set; }

    [State] public partial string Email { get; set; } = "";

    public override View Body => VStack(
        HasError ? Text("Adresse invalide") : null,
        TextField(Email, value => Email = value, "Email"));
}
