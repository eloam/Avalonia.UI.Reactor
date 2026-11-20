# Avalonia.UI.Reactor — UI déclarative en C#, sans XAML

Écriture façon SwiftUI, la plus concise possible : un composant = de l'état (`[State]`) + un `Body`.
Les actions s'écrivent directement dans la vue ; seul ce qui dépend de l'état modifié est recalculé.

> **Migration en cours vers Avalonia** : les vues seront rendues par les contrôles d'Avalonia, à la place de l'hôte
> actuel (SDL3 + Skia) décrit plus bas. Le code des applications (`Body`, `[State]`, modificateurs) ne change pas.

```csharp
Desktop.Run<CounterApp>("Compteur", 400, 300);

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
- Le layout d'une portée inchangée est réutilisé tel quel (pas de remesure du texte).
- Les champs ordinaires ne sont **pas** observés : utiliser `[State]`, ou `Refresh()` en dernier recours.
- `UiHost.LastUpdate` indique combien de portées ont été recalculées. Dans la démo : clic sur « + » → 1 (le compteur),
  une frappe → 1 (la ligne de saisie), cocher une tâche → 2 (la ligne et le pied de liste).

## Lancer la démo

```bash
dotnet run --project samples/TodoDemo
```

## Tests

```bash
dotnet test
```

[tests/Avalonia.UI.Reactor.Tests](tests/Avalonia.UI.Reactor.Tests) fait tourner le vrai runtime et le vrai moteur de texte Skia sans fenêtre
([TestHost.cs](tests/Avalonia.UI.Reactor.Tests/TestHost.cs) : `ClickButton("+")`, `Type("…")`, `HasText("…")`, `WaitUntil(…)`) :

- scénarios de la démo (compteur, saisie, coche, async, filtres, purge, thèmes) ;
- recalcul sélectif (`UiHost.LastUpdate`), thread de fond, `StateList`, composants imbriqués ;
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
- **Actions** : `Button("+", () => Count++)`, `Button("Charger", LoadAsync)`.
- **Liaisons** : un contrôle lié à un état le lit et le modifie lui-même — `TextField(Bind.Draft, "Nom")`, `Checkbox(todo.Bind.Done)`.
  `Bind` est généré pour chaque `[State]` modifiable ; il reste privé si les états le sont. Pour une valeur calculée : `TextField(valeur, v => …)`.
- **Asynchrone** : une action peut être `async Task` ; l'interface se met à jour avant et après chaque `await`, sans rien ajouter.
  ```csharp
  Button("Charger", Load)
  async Task Load() { Loading = true; Items = await api.GetAsync(); Loading = false; }
  ```
- **Erreurs** : une exception dans une action (synchrone, `async Task` ou même `async void`) ne ferme pas l'application ;
  elle est remontée à `UiHost.UnhandledException` (par défaut : écrite sur la sortie d'erreur).
- **Composants** : avec état, une classe `Component` ; sans état, une simple fonction qui renvoie une `View` (voir `Card` et `Segmented` dans la démo).
  Les modificateurs s'appliquent aussi aux composants : `counter.Padding(8).Background(ColorRole.Surface)`.
- **Affichage conditionnel** : `cond ? Text("…") : null` ; plusieurs vues d'un coup : `cond ? Group(a, b) : null`.

### Superposition et dessin libre

```csharp
// Superposer : le dernier enfant est au-dessus
ZStack(Avatar(), Text("JD"))
Icon().Overlay(Badge(3), Alignment.TopRight)

// Dessiner : couleurs fixes ou rôles du thème, la zone est dans d.Bounds
Canvas(d =>
{
    d.StrokeCircle(d.Bounds.Center, 18, ColorRole.Border, 4);
    d.Arc(d.Bounds.Center, 18, -90, 360 * progress, ColorRole.Accent, 4);
}).Size(40, 40)
```

`DrawScope` propose rectangles, cercles, arcs, lignes, polygones et texte ; `d.Context` donne accès au backend
pour le reste. Exemple complet : `ProgressRing` dans la démo — un contrôle sur mesure n'est qu'une fonction.

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
Desktop.Run<App>(new WindowOptions { Themes = new()
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
| `TextFieldTheme` | couleurs par état, largeur minimale, curseur (largeur, clignotement), modèle |
| `CheckboxTheme` | couleurs par état, taille, épaisseur de la coche, modèle |
| `Theme` | espacement par défaut des piles, séparateur (couleur, épaisseur), voiles de survol et d'appui |

- **Les vues utilisent des rôles** : `.Foreground(ColorRole.TextMuted)` s'adapte à tous les thèmes. Une couleur fixe reste possible : `.Background("#FF0000")`.
- **Les modificateurs gardent la priorité** sur le thème.

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

Sans modèle, le dessin standard s'applique. Le thème « Sable » de la démo ([DemoThemes.cs](samples/TodoDemo/DemoThemes.cs))
en utilise deux : des champs soulignés et des cases à cocher rondes.

## Fenêtre, polices et zoom

```csharp
Desktop.Run<App>(new WindowOptions
{
    Title = "Mon app", Width = 1024, Height = 768, MinWidth = 400,
    Resizable = true, Borderless = false, Maximized = false, Fullscreen = false, VSync = true,
    Themes = new() { { "Standard", Theme.Light, Theme.Dark }, { "Sable", sable, sableNuit } },
    Zoom = 1.25f,             // zoom de l'interface, en plus du DPI de l'écran
    ZoomShortcuts = true,     // Ctrl + / Ctrl − / Ctrl 0
    Graphics = GraphicsApi.Auto,  // Direct3D 12 sur Windows (OpenGL en repli), OpenGL ailleurs : même rendu
});

// Polices de l'application (.ttf / .otf), prioritaires sur celles du système
Fonts.Register("Inter", "Assets/Inter-Regular.ttf");
Fonts.Register("Inter", "Assets/Inter-SemiBold.ttf", weight: 600);
Fonts.RegisterResource("Inter", typeof(App).Assembly, "MonApp.Assets.Inter.ttf");   // police embarquée
var theme = Theme.Light.WithFont("Inter");
```

- Pour une graisse non fournie, la plus proche est utilisée ; si elle est trop légère, le gras est simulé.
- Le zoom se change aussi pendant l'exécution : `UiHost.Zoom`.
- Moteur GPU : Skia dessine la même image ; sur Windows, elle passe par Direct3D 12 (chaîne d'affichage DXGI « flip »),
  avec repli automatique sur OpenGL (message `[Reactor]`). Mesuré sur la démo : environ 20 % de processeur en moins par rendu
  qu'avec OpenGL, environ 20 Mo de mémoire privée en plus (tampons d'affichage), plateau stable.
- Au repos (aucun champ actif), la boucle de la fenêtre dort jusqu'au prochain événement : aucune consommation de processeur.

## Barre de menus

La barre se déclare dans le `Body`, comme une vue : coches, éléments grisés et listes suivent l'état.

```csharp
MenuBar(
    Menu("&Fichier",
        MenuItem("&Nouveau", New).Shortcut("Ctrl+N"),
        MenuItem("&Enregistrer", Save).Shortcut("Ctrl+S").Enabled(IsDirty),
        Divider(),
        Menu("&Récents", ForEach(recent, f => MenuItem(f.Name, () => Open(f)))),
        MenuItem("&Quitter", Desktop.Quit).Shortcut("Ctrl+Q")),
    Menu("&Affichage",
        MenuItem("Mode &sombre", Bind.DarkMode),                       // élément coché lié à un état
        MenuItem("&Clair", () => Scheme = Light).Checked(Scheme == Light)))
```

- `&` désigne la lettre d'accès (Alt+F ouvre « Fichier ») ; `&&` affiche un « & ».
- Les raccourcis (`"Ctrl+Shift+S"`, `"F5"`, `"Alt+F4"`) marchent menus fermés ; sur macOS, `Ctrl` devient Cmd.
  Pendant une saisie, un raccourci sans Ctrl, Alt, Cmd ni touche de fonction (`"P"`) laisse la lettre au champ.
- Clavier : F10 ou Alt seul activent la barre, flèches, Entrée, Échap, lettres d'accès ; la souris peut appuyer
  sur un titre, glisser jusqu'à l'élément et relâcher.
- Apparence : `Theme.WithMenu(m => m with { … })` (couleurs, marges, ombre, coche, surlignage),
  ou entièrement sur mesure avec `TitleTemplate` / `ItemTemplate`.

**Dessinée ou du système.** Par défaut, rien à écrire : barre dessinée sur Windows et Linux (même rendu partout,
suit le thème), barre du système sur macOS. Pour forcer : `new WindowOptions { MenuBar = MenuBarMode.Native }`
(menu Win32 sous la barre de titre sur Windows) ou `MenuBarMode.Custom`. La barre du système ne prend alors aucune
place dans la fenêtre ; les raccourcis restent gérés par le framework.

| Limite actuelle | Pourquoi |
|---|---|
| Barre du système de macOS pas encore faite : la barre dessinée la remplace (message `[Reactor]`) | il faut un hôte `net10.0-macos` (AppKit), à compiler sur un Mac |
| La barre Win32 reste claire en mode sombre | Windows n'offre pas de mode sombre officiel pour ses menus |
| Les menus dessinés restent dans la fenêtre (ils s'ouvrent vers le haut ou la gauche s'il le faut) | ils sont dessinés dans la fenêtre ; les en faire sortir demande des fenêtres surgissantes SDL |

## Architecture

| Projet | Rôle | Dépendances |
|---|---|---|
| `Avalonia.UI.Reactor.Core` | Vues (records immuables), DSL, modificateurs, thèmes, état réactif, layout avec recalcul sélectif, rendu des widgets, runtime (événements, focus, édition de texte, async, hot reload) | **aucune** |
| `Avalonia.UI.Reactor.Generators` | Générateur des propriétés `[State]` et analyseur de diagnostics (s'exécutent à la compilation) | Roslyn |
| `tests/Avalonia.UI.Reactor.Tests` | Tests sans fenêtre (xUnit) | xUnit |
| `Avalonia.UI.Reactor.Skia` | Dessin sur Skia (GPU, antialiasing, texte) — réutilisable tel quel sur mobile | SkiaSharp |
| `Avalonia.UI.Reactor.Desktop` | Fenêtre, moteur GPU (Direct3D 12 ou OpenGL sous Skia), entrées, presse-papiers, DPI, thème système, barre de menus Win32 | SDL3 (`ppy.SDL3-CS`), CsWin32 (déclarations Windows générées à la compilation) |
| `samples/TodoDemo` | Démo : compteur, todo list, action async, sélecteur de thème | — |

Un type par fichier, rangé par rôle :

```
src/Avalonia.UI.Reactor.Core/
  Primitives/   Color, Point, Size, Rect, Thickness, Font, Alignment… et les petites énumérations
  Theming/      Theme, ThemeFamily, Themes (catalogue), Palette, Typography, ColorValue, thèmes des contrôles
  Views/        View et ses records (TextView, ButtonView, StackView, ZStackView, CanvasView…),
                Ui (le DSL), Modifiers, ComponentModifiers
  State/        Component, [State], Signal, Scope, StateList
  Layout/       ViewTree (mesure + recalcul sélectif), LayoutEngine (placement, hit-test), LayoutNode
  Rendering/    IDrawingContext, DrawScope (Canvas), Renderer (dessin des widgets)
  Runtime/      UiHost (entrées, focus, texte), UiDispatcher (async, erreurs), HotReload,
                MenuController (menus ouverts, clavier, raccourcis), INativeMenuBar (barre du système)
samples/TodoDemo/
  Program.cs    Desktop.Run<TodoApp>(new WindowOptions { … })
  DemoThemes.cs familles Standard et Sable (Sable + Sable nuit, avec modèles de contrôles)
  Components/   TodoApp, CounterCard, TodoList
  Models/       Todo, Filter, Appearance
  Widgets.cs    Card, Segmented, ProgressRing (composants sans état)
tests/Avalonia.UI.Reactor.Tests/
  *Tests.cs     un fichier par thème ; Probes/ contient les petits composants de test
```

Chaque dossier est un espace de noms (`Avalonia.UI.Reactor.Views`, `Avalonia.UI.Reactor.State`, `Avalonia.UI.Reactor.Theming`…). Une application les importe une fois dans un `GlobalUsings.cs` (voir celui de la démo).

Le flux : `événement → action → état modifié → Body des seuls composants abonnés → layout (le reste vient du cache) → rendu`.
Survol, appui, focus et curseur texte sont gérés par le runtime, hors de l'état de l'application.
`UiHost.Elements` expose les éléments affichés et leur position, pour les tests automatisés.

SDL3 et Skia ne sont **pas** des frameworks UI : ce sont les couches fenêtre/entrées et dessin 2D (même approche que Flutter).

## Pistes suivantes

- **Menus** : barre du système sur macOS (hôte `net10.0-macos`), menus dessinés capables de sortir de la fenêtre
  (fenêtres surgissantes SDL, aussi utiles pour le menu contextuel et les listes déroulantes), menu contextuel.
- **Navigation** (`NavigationStack`, push/pop) : les composants imbriqués avec état sont en place, c'est la brique suivante.
- Listes virtualisées (ne construire que les lignes visibles), à faire avec le `ScrollView`.
- **Mobile** : hôtes Android/iOS réutilisant `Avalonia.UI.Reactor.Core` et `Avalonia.UI.Reactor.Skia` tels quels (rendu identique partout), puis tactile et clavier virtuel.
- `ScrollView`, texte multi-ligne, images, animations.
- Robustesse : barrière d'erreur dans les `Body`, cache de mesure du texte.
- Modèles pour les futurs contrôles (`ScrollView`, listes…), table de raccourcis clavier modifiable, caractère de troncature (`…`) réglable.
