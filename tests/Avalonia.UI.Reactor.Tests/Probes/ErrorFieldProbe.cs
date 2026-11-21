// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

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
