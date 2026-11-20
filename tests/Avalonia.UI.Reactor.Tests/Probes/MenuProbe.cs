using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests.Probes;

/// <summary>Barre à deux menus (sous-menu, séparateur, élément grisé, coche liée, raccourcis), un texte, un champ, un bouton.</summary>
public partial class MenuProbe : Component
{
    [State] public partial string Last { get; set; } = "";
    [State] public partial bool Dark { get; set; }
    [State] public partial bool CanSave { get; set; } = true;
    [State] public partial string Draft { get; set; } = "";

    public override View Body =>
        VStack(
            MenuBar(
                Menu("&Fichier",
                    MenuItem("&Nouveau", () => Last = "nouveau").Shortcut("Ctrl+N"),
                    MenuItem("&Enregistrer", () => Last = "enregistrer").Shortcut("Ctrl+S").Enabled(CanSave),
                    Divider(),
                    Menu("&Récents",
                        MenuItem("&A.txt", () => Last = "A"),
                        MenuItem("&B.txt", () => Last = "B")),
                    MenuItem("&Quitter", () => Last = "quitter")),
                Menu("&Affichage",
                    MenuItem("Mode &sombre", Bind.Dark),
                    MenuItem("&Rafraîchir", () => Last = "f5").Shortcut("F5"),
                    MenuItem("&Plein écran", () => Last = "plein").Shortcut("P"))),
            Text(Last).Key("last"),
            TextField(Bind.Draft, "Saisir…").Key("field"),
            HStack(Spacer(), Button("OK", () => Last = "ok"))); // à droite : hors du menu ouvert
}
