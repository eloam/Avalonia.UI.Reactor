// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// Two bound fields of the same type, the first one optional: each is identified by the property it is bound to.
/// </summary>
public sealed partial class CompanyFormProbe(bool bindEmailTwice = false) : Component
{
    [State] public partial bool IsCompany { get; set; }

    [State] public partial string Company { get; set; } = "";

    [State] public partial string Email { get; set; } = "";

    public override View Body => VStack(
        IsCompany ? TextField(Bind.Company, "Société") : null,
        TextField(Bind.Email, "Email"),
        bindEmailTwice ? TextField(Bind.Email, "Email encore") : null);
}
