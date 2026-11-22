// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A view written with view builder blocks: branches, a switch, nested blocks, a container of the application, view
/// methods and a list.
/// </summary>
public sealed partial class BlockProbe : Component
{
    [State] public partial bool LoggedIn { get; set; }

    [State] public partial int Page { get; set; }

    [State] public partial bool HasError { get; set; }

    [State] public partial string Email { get; set; } = "";

    public StateList<string> Items { get; } = ["a", "b"];

    public override View Body => VStack(() =>
    {
        Header();
        if (LoggedIn)
        {
            Text("Bienvenue").Bold();
        }
        else
        {
            Text("Invité");
            Button("Connexion", () => LoggedIn = true);
        }

        switch (Page)
        {
            case 0:
                Text("Accueil");
                break;
            default:
                Text($"Page {Page}");
                break;
        }

        HStack(() => Text("Ligne"));
        Card(() => { Text("Dans la carte"); });
        Details();
        Summary();
        if (HasError)
            Text("Erreur");

        TextField(Bind.Email, "Email");
        ForEach(Items, item =>
        {
            Text(item);
            if (item == "b")
                Text("b!");
        });
        Footer();
    });

    private static StackView Card([ViewBuilder] Action content) => VStack(content).Padding(8);

    private static View Footer() => Text("Pied");

    private View Header() => Text(Page == 0 ? "En-tête" : $"En-tête {Page}");

    private View Details() => Group(() =>
    {
        Text("Détail 1");
        if (LoggedIn)
            Text("Détail 2");
    });

    // A local variable and an early exit, written with a named argument.
    private View Summary() => HStack(content: () =>
    {
        string count = $"{Items.Count} éléments";
        Text(count);
        if (Items.Count == 0)
            return;

        Text("Liste non vide");
    });
}
