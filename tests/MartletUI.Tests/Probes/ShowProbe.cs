// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

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
