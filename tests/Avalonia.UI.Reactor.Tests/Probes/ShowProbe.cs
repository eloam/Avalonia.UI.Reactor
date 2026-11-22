// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A block that adds components, a switch expression and a null view with <c>Show</c>.
/// </summary>
public sealed partial class ShowProbe : Component
{
    [State] public partial bool ShowFirst { get; set; }

    [State] public partial int Page { get; set; }

    public CounterProbe First { get; } = new("a");

    public CounterProbe Second { get; } = new("b");

    public override View Body => VStack(() =>
    {
        if (ShowFirst)
            Show(First);

        Show(Second);
        Show(Page switch
        {
            0 => Text("Accueil"),
            _ => Text($"Page {Page}"),
        });
        Show(null);
    });
}
