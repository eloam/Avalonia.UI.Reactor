// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Theming;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A text whose background shows only while it is highlighted.
/// </summary>
public sealed partial class HighlightProbe : Component
{
    [State] public partial bool Highlighted { get; set; }

    public override View Body =>
        VStack(
            Highlighted ? Text("mot").Background(ColorRole.Accent) : Text("mot"),
            Button("Surligner", () => Highlighted = !Highlighted));
}
