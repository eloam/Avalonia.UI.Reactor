// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

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
