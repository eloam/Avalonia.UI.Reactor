# Avalonia.UI.Reactor — UI déclarative en C#, sans XAML

Écriture façon SwiftUI, la plus concise possible : un composant = de l'état (`[State]`) + un `Body`.
Les actions s'écrivent directement dans la vue ; seul ce qui dépend de l'état modifié est recalculé.
Le rendu est confié aux **contrôles d'Avalonia**, dessinés par Skia : même image sur Windows, macOS et Linux.

```csharp
ReactorApp.Run<CounterApp>("Compteur", 400, 300);

public sealed partial class CounterApp : Component
{
    [State] partial int Count { get; set; }

    public override View Body =>
        HStack(
            Button("−", () => Count--).Style(ButtonStyle.Secondary),
            Text($"{Count}").Font(TextStyle.Title),
            Button("+", () => Count++),
            Count != 0 ? Button("Remettre à zéro", () => Count = 0).Style(ButtonStyle.Ghost) : null)
        .Padding(24);
}
```

## État réactif

Comme `$state` en Svelte, `@State`/`@Observable` en SwiftUI ou `mutableStateOf` en Compose :
une vue qui **lit** un état s'y abonne, et le **modifier** ne recalcule que les vues qui l'ont lu.

| Écriture | Rôle |
|---|---|
| `[State] partial int Count { get; set; }` | État observé (valeur initiale possible : `{ get; set; } = "";`). La classe doit être `partial`. |
| `StateList<Todo> todos = [a, b];` | Liste observée : `Add`, `Remove`, `RemoveAll`… recalculent ce qui la parcourt. |
| `[State]` sur une classe modèle (`Todo.Done`) | Cocher une tâche ne recalcule que sa ligne (et ce qui compte les tâches). |
| `VStack(counter, todoList)` | Un composant s'insère comme une vue ; il garde son état et n'est recalculé que s'il le faut. |
| `ForEach(todos, Row, t => t.Id)` | Une portée par élément de liste. |
| `TextField(Bind.Draft)`, `Checkbox(todo.Bind.Done)` | Liaison à double sens, générée pour chaque `[State]` modifiable (comme `$draft` en SwiftUI). |
| `Observe(() => …)` | Isole un bloc d'un `Body` sans créer de composant (ex. la ligne de saisie). |

- Le code des propriétés est écrit à la compilation par un générateur Roslyn ([StateGenerator.cs](src/Avalonia.UI.Reactor.Generators/StateGenerator.cs)),
  l'équivalent du compilateur de Svelte. **Rien à configurer** : il est embarqué dans le package `Avalonia.UI.Reactor.Core`
  (et branché automatiquement dans ce dépôt par [Directory.Build.targets](Directory.Build.targets)).
- Un analyseur ([StateAnalyzer.cs](src/Avalonia.UI.Reactor.Generators/StateAnalyzer.cs)) signale les erreurs dès la compilation :

  | Code | Cas |
  |---|---|
  | `MVU001` | `[State]` sur une propriété non `partial` |
  | `MVU002` | `[State]` dans une classe non `partial` |
  | `MVU003` | `[State]` avec un corps, `static`, ou sans `get` |
  | `MVU004` | `Bind` ou `StateBindings` déclaré à la main dans une classe qui a des états : noms réservés aux liaisons générées |
  | `MVU010` | ⚠️ champ affiché par la vue, modifié, mais pas `[State]` : l'écran ne suivrait pas |
  | `MVU011` | ⚠️ `List<T>` affichée par la vue et modifiée : utiliser `StateList<T>` |
  | `MVU012` | état passé à un contrôle sans sa liaison (`TextField(Name)`, `Checkbox(todo.Done)`) : le message donne `Bind.Name`, `todo.Bind.Done` |
- Un état peut changer **de n'importe où** — action, après un `await`, minuteur, thread de fond — sans appel à `Refresh()`.
- Une portée inchangée garde ses contrôles tels quels : ni recalcul de son `Body`, ni mise à jour de ses contrôles.
- Les champs ordinaires ne sont **pas** observés : utiliser `[State]`, ou `Refresh()` en dernier recours.
- Dans la démo : clic sur « + » → 1 portée recalculée (le compteur), une frappe → 1 (la ligne de saisie),
  cocher une tâche → 2 (la ligne et le pied de liste). Les tests le vérifient (`Stats`).

## Lancer la démo

```bash
dotnet run --project samples/TodoDemo
```

## Tests

```bash
dotnet test
```

[tests/Avalonia.UI.Reactor.Tests](tests/Avalonia.UI.Reactor.Tests) ouvre de vraies fenêtres Avalonia, sans écran
(Avalonia.Headless, rendu par Skia) : [ReactorTestHost.cs](tests/Avalonia.UI.Reactor.Tests/ReactorTestHost.cs)
clique, survole et tape par les entrées de la plateforme (`ClickButton("+")`, `Type("…")`, `Press(…)`), lit les
textes, les positions et les pixels (`TextShowing("…")`, `BoundsOf(…)`, `PixelAt(x, y)`) :

- scénarios de la démo (compteur, saisie, coche, async, filtres, purge, thèmes) ;
- réconciliateur (contrôles repris d'une mise à jour à l'autre), recalcul sélectif, thread de fond, `StateList` ;
- mise en page des piles, thèmes et familles, apparence des contrôles état par état, modèles, menus, zoom ;
- durée de vie (une fenêtre fermée est libérée) ;
- générateur et diagnostics, sur des extraits de code compilés en mémoire.

## Hot reload

```bash
dotnet watch --project samples/TodoDemo
```

Modifier une vue (`Body`, une méthode, une lambda) et enregistrer : la fenêtre se met à jour en ~1 s,
**sans perdre l'état** (compteur, saisie, liste…). Fonctionne aussi avec le Hot Reload de Visual Studio et Rider.

- Branchement : [HotReload.cs](src/Avalonia.UI.Reactor.Core/Runtime/HotReload.cs) — un `MetadataUpdateHandler` qui réévalue tous les composants de chaque fenêtre ouverte.
- Non rechargé à chaud : les champs `static readonly` déjà initialisés (ex. un thème statique), et les modifications
  que .NET refuse (changer une signature, renommer une classe…) : `dotnet watch` redémarre alors l'application.

## Écrire des vues

- **Primitives** (`using static Avalonia.UI.Reactor.Views.Ui;`) : `Text`, `Button`, `TextField`, `Checkbox`, `VStack`, `HStack`, `ZStack`, `Group`, `ForEach`, `Canvas`, `Spacer`, `Divider`, `Box`.
- **Piles** : les enfants d'abord, le reste en modificateurs. Une pile **s'étire toute seule** si un enfant s'étire
  (`Spacer()`, `.Grow()`, `.FillWidth()`) : inutile d'écrire `.FillWidth()` sur la pile.
  ```csharp
  VStack(Header(), ForEach(todos, Row), Footer())       // ForEach se mélange aux autres enfants
      .Spacing(16)                                      // sinon : l'espacement du thème
  HStack(Text("Tâches"), Spacer(), Badge())              // le Spacer étire la ligne, le badge part à droite
  ZStack(Icon(), Badge()).Alignment(Alignment.TopRight)
  ```
- **Modificateurs chaînables** : `.Padding()`, `.Background()`, `.Border()`, `.Radius()`, `.Foreground()`, `.Font(TextStyle.Title)`, `.Grow()`, `.FillWidth()`, `.OnTap(() => …)`, `.Key()`…
  Les arguments nommés lèvent toute ambiguïté : `.Padding(horizontal: 6, vertical: 4)`.
  Une bordure prend sa place, comme en CSS : avec `.Padding(16).Border(…, 2)`, le contenu commence à 18.
- **Actions** : `Button("+", () => Count++)`, `Button("Charger", LoadAsync)`.
- **Liaisons** : un contrôle lié à un état le lit et le modifie lui-même — `TextField(Bind.Draft, "Nom")`, `Checkbox(todo.Bind.Done)`.
  `Bind` est généré pour chaque `[State]` modifiable ; il reste privé si les états le sont. Pour une valeur calculée : `TextField(valeur, v => …)`.
  Champs et cases sont **contrôlés** : la saisie part vers l'état, puis le contrôle affiche la valeur de sa vue (un état
  qui refuse ou transforme la saisie l'emporte).
- **Asynchrone** : une action peut être `async Task` ; l'interface se met à jour avant et après chaque `await`, sans rien ajouter.
  ```csharp
  Button("Charger", Load)
  async Task Load() { Loading = true; Items = await api.GetAsync(); Loading = false; }
  ```
- **Erreurs** : une exception dans une action (synchrone, `async Task` ou même `async void`) ne ferme pas l'application ;
  elle est écrite sur la sortie d'erreur et dans le débogueur (préfixe `[Reactor]`).
- **Composants** : avec état, une classe `Component` ; sans état, une simple fonction qui renvoie une `View` (voir `Card` et `Segmented` dans la démo).
  Les modificateurs s'appliquent aussi aux composants : `counter.Padding(8).Background(ColorRole.Surface)`.
- **Affichage conditionnel** : `cond ? Text("…") : null` ; plusieurs vues d'un coup : `cond ? Group(a, b) : null`.
- **Texte** : une ligne trop longue pour sa place se termine par « … ».

### Superposition et dessin libre

```csharp
// Superposer : le dernier enfant est au-dessus (et reçoit le clic)
ZStack(Avatar(), Text("JD"))
Icon().Overlay(Badge(3), Alignment.TopRight)

// Dessiner : couleurs fixes ou rôles du thème, la zone est dans d.Bounds
Canvas(d =>
{
    d.StrokeCircle(d.Bounds.Center, 18, ColorRole.Border, 4);
    d.Arc(d.Bounds.Center, 18, -90, 360 * progress, ColorRole.Accent, 4);
}).Size(40, 40)
```

`DrawScope` propose rectangles, cercles, arcs, lignes, polygones et texte ; `d.Context` donne accès au contexte de
dessin pour le reste (ombres, découpes). Un `Canvas` suit les états lus **pendant son dessin** : il est redessiné seul
quand l'un d'eux change. Exemple complet : `ProgressRing` dans la démo — un contrôle sur mesure n'est qu'une fonction.

### Un contrôle Avalonia quelconque

Un contrôle d'Avalonia sans équivalent dans le DSL s'intègre tel quel ; c'est la seule API qui expose un type Avalonia.

```csharp
using static Avalonia.UI.Reactor.AvaloniaViews;

Native(() => new Calendar(), calendar => calendar.DisplayDate = Today)   // créé une fois, accordé à l'état
```

La mise à jour suit les états qu'elle lit : elle est refaite seule quand l'un d'eux change.

## Thèmes

Aucune couleur ni dimension n'est codée en dur : tout vient d'un `Theme` ([Theming/](src/Avalonia.UI.Reactor.Core/Theming)).
Clair et Sombre sont fournis ; un thème à soi se crée en partant de l'un d'eux et en ne changeant que ce qui diffère.

```csharp
var sable = Theme.Light.Named("Sable")          // partir du clair → thème clair
    .WithColor(ColorRole.Page, "#FBF7F2")
    .WithAccent("#C2410C")
    .WithFont("Georgia", bodySize: 15)
    .WithRadius(12);
var sableNuit = Theme.Dark.Named("Sable nuit")  // partir du sombre → thème sombre
    .WithAccent("#F97316") /* … */;
```

**Le catalogue** — les thèmes de l'application, regroupés en familles (un thème clair et/ou un sombre sous un nom) :

```csharp
ReactorApp.Run<App>(new WindowOptions { Themes = new()
{
    { "Standard", Theme.Light, Theme.Dark },   // la première famille habille l'application par défaut
    { "Sable", sable, sableNuit },             // clair ou sombre selon le mode en cours
    { "Contraste", contraste },                // un seul thème : toujours celui-là
}});
```

**Appliquer un thème** — c'est la vue qui le déclare, comme n'importe quel autre style :

```csharp
Button("OK").Theme("Sable")              // un seul bouton dans un autre thème
counter.Theme("Sable")                   // un composant entier
Card(…).ColorScheme(ColorScheme.Dark)    // la variante sombre de la famille en cours
Text("…").Theme(sableNuit)               // un thème précis, tel quel
```

**Laisser l'utilisateur choisir** — son choix est un état comme un autre ; la vue en découle :

```csharp
[State] partial string ThemeName { get; set; } = "Standard";
[State] partial ColorScheme? Scheme { get; set; }        // null = suit le système

public override View Body => Page()
    .Theme(ThemeName)          // tout l'écran dans la famille choisie…
    .ColorScheme(Scheme);      // …en clair, en sombre, ou selon le système
```

- Une famille à deux variantes suit le mode du système, y compris s'il change en cours de route.
- Une vue sans thème hérite de celui de son parent (famille **et** mode) ; un nom inconnu du catalogue est signalé sur la sortie d'erreur et la vue garde le thème hérité.
- `Themes.Families` liste le catalogue, par exemple pour construire un sélecteur : celui de la démo ajoute tout seul une famille ajoutée au catalogue.

**Ajouter ses propres jetons** — couleurs, styles de texte et styles de bouton nommés, utilisés ensuite par leur nom :

```csharp
theme.WithColor("success", "#16A34A")                      // .Background("success"), .Foreground("success")
     .WithTextStyle("caption", size: 12)                   // .Font("caption")
     .WithButtonStyle("success", new("success", ColorRole.OnAccent));   // .Style("success")
```

Un nom de couleur absent du thème s'affiche en magenta, pour repérer l'oubli tout de suite.

| Niveau | Contenu |
|---|---|
| `Palette` | rôles (`Page`, `Surface`, `Border`, `Text`, `TextMuted`, `Accent`, `Danger`…) + couleurs nommées |
| `Typography` | police (par défaut celle du système), taille et graisse de chaque style, styles nommés |
| `ButtonTheme` | padding, rayon, police, variantes `Primary` / `Secondary` / `Ghost` / `Danger` + styles nommés, modèle |
| `TextFieldTheme` | couleurs par état, largeur minimale, curseur (couleur, clignotement), sélection, libellés du menu contextuel, modèle |
| `CheckboxTheme` | couleurs par état, taille, épaisseur de la coche, modèle |
| `MenuTheme` | barre de menus dessinée : fond, trait du bas, marges |
| `Theme` | espacement par défaut des piles, séparateur (couleur, épaisseur), voiles de survol et d'appui |

- **Les vues utilisent des rôles** : `.Foreground(ColorRole.TextMuted)` s'adapte à tous les thèmes. Une couleur fixe reste possible : `.Background("#FF0000")`.
- **Les modificateurs gardent la priorité** sur le thème.
- Bouton, case et champ portent **notre** apparence (une structure seule, sans le thème Fluent) : leurs couleurs par
  état (repos, survol, appui, focus) viennent du thème de leur branche.

### Modèles de contrôles

Changer les couleurs ne suffit pas toujours : un **modèle** remplace l'apparence entière d'un contrôle par une vue,
calculée à partir de son état. Le comportement (clic, coche, saisie, focus) ne change pas.

```csharp
// Pour tous les boutons du thème…
theme.WithButton(b => b with
{
    Template = s => HStack(Text("★"), Text(s.Label))
        .Padding(14, 8)
        .Background(s.Pressed ? ColorRole.Accent : s.Variant.Background)
        .Radius(999),
});

// …ou pour un seul contrôle
Checkbox(done, v => done = v).Template(s => Text(s.IsChecked ? "☑" : "☐"));

// Un champ : le modèle dessine le cadre et place s.Editor là où le texte et le curseur s'affichent
theme.WithTextField(f => f with
{
    Template = s => VStack(s.Editor, Divider().Height(2).Background(s.Focused ? ColorRole.Accent : ColorRole.Border)).Spacing(4),
});
```

| Contrôle | Le modèle reçoit |
|---|---|
| `Button` | `Label`, `Variant` (couleurs du style choisi), `Hovered`, `Pressed` |
| `Checkbox` | `IsChecked`, `Label`, `Hovered`, `Pressed` |
| `TextField` | `Value`, `Placeholder`, `Focused`, `Hovered`, `Editor` |

Sans modèle, le dessin standard s'applique. Le modèle donne aussi sa taille au contrôle. Le thème « Sable » de la démo
([DemoThemes.cs](samples/TodoDemo/DemoThemes.cs)) en utilise deux : des champs soulignés et des cases à cocher rondes.

## Fenêtre et zoom

```csharp
ReactorApp.Run<App>(new WindowOptions
{
    Title = "Mon app", Width = 1024, Height = 768, MinWidth = 400,
    Resizable = true, Borderless = false, Maximized = false, Fullscreen = false,
    Themes = new() { { "Standard", Theme.Light, Theme.Dark }, { "Sable", sable, sableNuit } },
    Zoom = 1.25,              // zoom de l'interface (de 0,5 à 3), en plus du facteur d'échelle de l'écran
    ZoomShortcuts = true,     // Ctrl + / Ctrl − / Ctrl 0 (Cmd sur macOS), par pas de 10 %
});
```

- `ReactorApp.Run(new MonApp(paramètres), options)` ouvre un composant déjà construit ; `ReactorApp.Quit()` ferme l'application.
- Polices : `Theme.WithFont("Inter")` désigne une police installée sur le système.
- Au repos, aucune passe de mise en page : la fenêtre ne travaille que lorsqu'un état change.

## Barre de menus

La barre se déclare dans le `Body`, comme une vue : coches, éléments grisés et listes suivent l'état.

```csharp
MenuBar(
    Menu("&Fichier",
        MenuItem("&Nouveau", New).Shortcut("Ctrl+N"),
        MenuItem("&Enregistrer", Save).Shortcut("Ctrl+S").Enabled(IsDirty),
        Divider(),
        Menu("&Récents", ForEach(recent, f => MenuItem(f.Name, () => Open(f)))),
        MenuItem("&Quitter", ReactorApp.Quit).Shortcut("Ctrl+Q")),
    Menu("&Affichage",
        MenuItem("Mode &sombre", Bind.DarkMode),                       // élément coché lié à un état
        MenuItem("&Clair", () => Scheme = Light).Checked(Scheme == Light)))
```

- `&` désigne la lettre d'accès (Alt+F ouvre « Fichier ») ; `&&` affiche un « & ».
- Les raccourcis (`"Ctrl+Shift+S"`, `"F5"`, `"Alt+F4"`) marchent menus fermés ; sur macOS, `Ctrl` devient Cmd.
  Pendant une saisie, un raccourci sans Ctrl, Alt, Cmd ni touche de fonction (`"P"`) laisse la lettre au champ.
- **Rien à choisir** : sur macOS, les menus vont dans la barre du système (en haut de l'écran, la barre ne prend
  aucune place dans la fenêtre) ; ailleurs, la barre est dessinée en haut de la fenêtre, aux couleurs du thème
  (`Theme.WithMenu(m => m with { BarBackground = …, BarPadding = … })`).
- Un champ de saisie a un menu contextuel couper / copier / coller, libellé par le thème (`TextFieldTheme.EditMenu`).

Limite actuelle : les menus ouverts ont l'apparence d'Avalonia (thème Fluent), pas encore celle de notre thème.

## Comment ça marche

Un **réconciliateur** applique l'arbre de vues à des contrôles Avalonia qu'il garde d'une mise à jour à l'autre : un
contrôle est repris tant que sa vue garde le même chemin (son `Key`, sinon son rang) ; seules ses propriétés changent.
Il garde donc focus, survol et curseur, et une liste réordonnée déplace ses contrôles au lieu de les recréer.

```
événement Avalonia (clic, saisie) → action → état modifié → portées abonnées marquées
  → Body des seules portées marquées → chaque vue appliquée au contrôle qui occupait sa place (propriétés changées seulement)
  → mise en page et rendu d'Avalonia
```

Les piles sont un panneau à nous (`FlexStack`), qui suit les règles de SwiftUI : enfants à leur taille naturelle,
`Spacer` et `.Grow()` se partagent la place restante, une pile s'étire si un enfant s'étire.

## Architecture

| Projet | Rôle | Dépendances |
|---|---|---|
| `Avalonia.UI.Reactor.Core` | API des applications : vues (records immuables), DSL, modificateurs, thèmes, état réactif, portées, hot reload | **aucune** |
| `Avalonia.UI.Reactor` | `ReactorApp`, `ReactorHost`, réconciliateur (vues → contrôles), panneaux, menus, nos contrôles et leur apparence | Avalonia 12 |
| `Avalonia.UI.Reactor.Generators` | Générateur des propriétés `[State]` et analyseur de diagnostics (s'exécutent à la compilation) | Roslyn |
| `samples/TodoDemo` | Démo : compteur, todo list, action async, sélecteur de thème, barre de menus | — |
| `tests/Avalonia.UI.Reactor.Tests` | Tests dans de vraies fenêtres sans écran, et tests du générateur | xUnit v3, Avalonia.Headless |

Un type par fichier, rangé par rôle :

```
src/Avalonia.UI.Reactor.Core/
  Primitives/      Color, Point, Size, Rect, Thickness, Font, Alignment, KeyGesture… et les petites énumérations
  Theming/         Theme, ThemeFamily, Themes (catalogue), Palette, Typography, ColorValue, thèmes des contrôles,
                   ViewEnvironment (thème, police et couleur hérités)
  Views/           View et ses records (TextView, ButtonView, StackView, ZStackView, CanvasView…),
                   Ui (le DSL), Modifiers, ComponentModifiers, ControlTemplates, MenuShortcuts
  State/           Component, [State], Signal, Scope, StateList, Binding
  Rendering/       IDrawingContext et DrawScope (Canvas), CheckboxGlyph
  Runtime/         HotReload
src/Avalonia.UI.Reactor/
  ReactorApp.cs    point d'entrée, WindowOptions, AvaloniaViews (Native)
  Hosting/         ReactorHost (mises à jour, erreurs, zoom, raccourcis), ReactorApplication
  Reconciliation/  Reconciler, Node, un adaptateur par type de vue (TextAdapter, ButtonAdapter…)
  Panels/          FlexStack (piles), LayerStack (ZStack)
  Menus/           barre dessinée (Menu d'Avalonia) ou barre du système, synchronisation avec les vues
  Widgets/         ReactorButton, ReactorCheckBox, ReactorTextBox, leur apparence (ReactorThemes), Surface, DrawingCanvas
samples/TodoDemo/
  Program.cs       ReactorApp.Run<TodoApp>(new WindowOptions { … })
  DemoThemes.cs    familles Standard et Sable (Sable + Sable nuit, avec modèles de contrôles)
  Components/      TodoApp, CounterCard, TodoList
  Models/          Todo, Filter
  Widgets.cs       Card, Segmented, ProgressRing (composants sans état)
tests/Avalonia.UI.Reactor.Tests/
  *Tests.cs        un fichier par sujet ; Probes/ contient les petits composants de test
```

Chaque dossier est un espace de noms (`Avalonia.UI.Reactor.Views`, `Avalonia.UI.Reactor.State`, `Avalonia.UI.Reactor.Theming`…).
Une application les importe une fois dans un `GlobalUsings.cs` (voir celui de la démo).

## Pistes suivantes

- `ScrollView` et listes virtualisées (ne construire que les lignes visibles), texte multiligne.
- **Navigation** (`NavigationStack`, push/pop) : les composants imbriqués avec état sont en place, c'est la brique suivante.
- Menus ouverts aux couleurs du thème, modèles de menus, menu contextuel déclaré dans la vue.
- Polices embarquées dans l'application, images, animations, accessibilité.
- **Mobile et navigateur** : hôtes Android, iOS et WebAssembly d'Avalonia, même code d'application.
- Robustesse : barrière d'erreur dans les `Body`.
