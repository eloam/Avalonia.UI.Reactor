// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

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
