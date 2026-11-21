// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

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
