namespace Avalonia.UI.Reactor.Headless.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Barre de menus : raccourcis, élément grisé, coche liée à un état, sous-menu, séparateur, et un champ.</summary>
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
