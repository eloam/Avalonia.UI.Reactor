// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A component whose Body is a group of two views.
/// </summary>
public sealed class GroupBodyProbe : Component
{
    public override View Body => Group(Text("c1"), Text("c2"));
}
