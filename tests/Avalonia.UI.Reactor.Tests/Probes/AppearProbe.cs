// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A view with OnAppear and OnDisappear, in a branch that opens and closes, which can gain a background.
/// </summary>
public sealed partial class AppearProbe : Component
{
    [State] public partial bool Shown { get; set; } = true;

    [State] public partial bool Highlighted { get; set; }

    [State] public partial int Count { get; set; }

    public int Appeared { get; private set; }

    public int Disappeared { get; private set; }

    public override View Body
    {
        get
        {
            TextView watched = Text("Observée").OnAppear(() => Appeared++).OnDisappear(() => Disappeared++);
            return VStack(
                Text($"Compte {Count}"),
                Shown ? (Highlighted ? watched.Background(ColorRole.Accent) : watched) : null);
        }
    }
}
