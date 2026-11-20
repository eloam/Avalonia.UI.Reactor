using Mvu.Desktop;
using Mvu.Primitives;
using Mvu.State;
using Mvu.Theming;
using Mvu.Views;

namespace TodoDemo;

/// <summary>
/// Racine : la barre de menus, l'en-tête avec le choix du thème, et deux composants qui ont chacun leur état.
/// Le thème choisi est un état comme un autre : la vue en découle, rien n'est appelé pour l'appliquer.
/// </summary>
public sealed partial class TodoApp : Component
{
    [State] partial string ThemeName { get; set; } = "Standard";

    /// <summary>Mode imposé ; null = celui du système.</summary>
    [State]
    partial ColorScheme? Scheme { get; set; }

    [State] partial bool ShowCounter { get; set; } = true;
    [State] partial bool ShowAbout { get; set; }

    readonly CounterCard counter = new();
    readonly TodoList todos = new();

    public override View Body =>
        VStack(
                Observe(Menus), // bloc à part : cocher une tâche ne recalcule que la barre, pas tout l'écran
                VStack(Header(), ShowAbout ? About() : null, ShowCounter ? counter : null, todos)
                    .Spacing(16)
                    .Padding(24))
            .Spacing(0)
            .Background(ColorRole.Page)
            .Theme(ThemeName) // tout l'écran dans la famille choisie…
            .ColorScheme(Scheme); // …en clair, en sombre, ou selon le système

    /// <summary>
    /// Dessinée en haut de la fenêtre, ou barre du système selon <c>WindowOptions.MenuBar</c>.
    /// Les coches, les éléments grisés et les listes suivent l'état, comme le reste de la vue.
    /// </summary>
    View Menus() =>
        MenuBar(
            Menu("&Fichier",
                MenuItem("&Nouvelle idée", todos.Suggest).Shortcut("Ctrl+N"),
                MenuItem("&Purger les tâches faites", todos.Purge).Shortcut("Ctrl+Shift+P").Enabled(todos.HasDone),
                Divider(),
                MenuItem("&Quitter", Desktop.Quit).Shortcut("Ctrl+Q")),
            Menu("&Affichage",
                Menu("&Thème", [.. Themes.Families.Select(f =>
                    MenuItem(f.Name, () => ThemeName = f.Name).Checked(ThemeName == f.Name))]),
                Menu("&Mode",
                    MenuItem("&Système", () => Scheme = null).Checked(Scheme is null),
                    MenuItem("&Clair", () => Scheme = ColorScheme.Light).Checked(Scheme == ColorScheme.Light),
                    MenuItem("&Sombre", () => Scheme = ColorScheme.Dark).Checked(Scheme == ColorScheme.Dark)),
                Divider(),
                MenuItem("&Compteur", Bind.ShowCounter).Shortcut("Ctrl+K")),
            Menu("&Aide",
                MenuItem("À &propos", () => ShowAbout = true).Shortcut("F1")));

    View Header() =>
        VStack(
                HStack(
                        Text("POC").Font(TextStyle.Title),
                        Spacer(), // la ligne prend toute la largeur : le sélecteur part à droite
                        // Familles lues dans le catalogue : en ajouter une l'ajoute ici.
                        Segmented(Bind.ThemeName, [.. Themes.Families.Select(f => (f.Name, f.Name))]),
                        Segmented(Bind.Scheme,
                            ("Système", null), ("Clair", ColorScheme.Light), ("Sombre", ColorScheme.Dark)))
                    .Spacing(12),
                Text("UI déclarative en C# — ni XAML, ni framework tiers").Foreground(ColorRole.TextMuted))
            .Spacing(2);

    View About() =>
        Card(
            Text("À propos").Font(TextStyle.Headline),
            Text("Framework UI maison : SDL3 pour la fenêtre, Skia pour le dessin, le même rendu partout.")
                .Foreground(ColorRole.TextMuted),
            HStack(Spacer(), Button("Fermer", () => ShowAbout = false).Style(ButtonStyle.Secondary)));
}
