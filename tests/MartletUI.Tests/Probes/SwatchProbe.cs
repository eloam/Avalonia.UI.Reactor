// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A drawn swatch whose color depends on a state.
/// </summary>
public sealed partial class SwatchProbe : Component
{
    [State] public partial bool Red { get; set; }

    public override View Body =>
        VStack(
            Canvas(d => d.FillRect(d.Bounds, Red ? "#FF0000" : "#0000FF")).Size(40, 20),
            Button("Rouge", () => Red = true));
}
