// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A menu bar: shortcuts, a disabled item, a check mark bound to state, a submenu, a separator, and a field.
/// </summary>
public sealed partial class MenuProbe : Component
{
    [State] public partial string Last { get; set; } = "";

    [State] public partial bool CanSave { get; set; } = true;

    [State] public partial bool ShowCounter { get; set; }

    [State] public partial string Draft { get; set; } = "";

    public override View Body =>
        VStack(
            MenuBar(
                Menu("&Fichier",
                    MenuItem("&Nouveau", () => Last = "nouveau").Shortcut("Ctrl+N"),
                    MenuItem("&Enregistrer", () => Last = "enregistrer").Shortcut("Ctrl+S").Enabled(CanSave),
                    Divider(),
                    Menu("&Récents", MenuItem("rapport.txt", () => Last = "rapport"))),
                Menu("&Affichage",
                    MenuItem("&Compteur", Bind.ShowCounter).Shortcut("F5"),
                    MenuItem("&Plein écran", () => Last = "plein").Shortcut("P"))),
            TextField(Bind.Draft),
            Text("dernier").Key("last"));
}
