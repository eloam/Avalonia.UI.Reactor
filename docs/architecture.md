# Architecture

## How it works

A **reconciler** applies the tree of views to Avalonia controls that it keeps from one update to the next. A control
is reused as long as its view keeps the same place and the same kind; only its changed properties are set. The place of
a child comes from its `Key`, otherwise from an identity it already has (a component by its instance, a control bound to
a property by that property), otherwise from its view type and its rank among the children of that type: a view of
another type that appears before it does not shift it. Controls therefore keep focus, hover and caret, and a reordered
list moves its controls instead of recreating them.

```
Avalonia event (click, typing) → action → state changed → subscribed scopes marked
  → Body of the marked scopes only → each view applied to the control that held its place (changed properties only)
  → Avalonia layout and rendering
```

- **Scopes**: a component, a `ForEach` item, an `Observe` block, the drawing of a `Canvas` and the update of a `Native`
  view each record the state they read. Only a change to one of those re-evaluates them.
- **Threads**: state can change from any thread; the tree and the controls are only touched on the UI thread.
- **Stacks** are our own Avalonia panel (`FlexStack`), which follows SwiftUI's rules.
- **Controls**: `ReactorButton`, `ReactorCheckBox` and `ReactorTextBox` derive from Avalonia's `Button`, `CheckBox` and
  `TextBox`. They set their own structure-only `ControlTheme`; adapters compute their colors for each state from the
  theme. Fluent is only loaded for the window infrastructure, scroll bars and open menus.

| View | Avalonia control |
|---|---|
| `Text` | `TextBlock` |
| `Button` | `ReactorButton` (a `Button`) |
| `TextField` | `ReactorTextBox` (a `TextBox`) |
| `Checkbox` | `ReactorCheckBox` (a `CheckBox`) |
| `MenuBar`, `Menu`, `MenuItem` | `Menu`, `MenuItem`; `NativeMenu` for the macOS system menu bar |
| `VStack`, `HStack` | `FlexStack` (a `Panel`) |
| `ZStack`, `.Overlay` | `LayerStack` (a `Panel`) |
| `Divider` | `Border` |
| `Box`, decorations, `.OnTap` | `Surface` (a `Border`) |
| `Canvas` | `DrawingCanvas` (a `Control` drawing with Avalonia's `DrawingContext`) |
| `Native(…)` | the control you provide |

## Projects

| Project | Role | Depends on |
|---|---|---|
| `Avalonia.UI.Reactor.Core` | The API of applications: views, DSL, modifiers, themes, reactive state, scopes, hot reload | nothing |
| `Avalonia.UI.Reactor` | `ReactorApp`, `ReactorHost`, the reconciler, panels, menus, our controls and their look | Avalonia 12 |
| `Avalonia.UI.Reactor.Generators` | Source generators of `[State]` and of view builder blocks, and the diagnostics analyzer, run at compile time | Roslyn |
| `samples/TodoDemo` | Sample app: counter, task list, async action, theme picker, menu bar | — |
| `tests/Avalonia.UI.Reactor.Tests` | Tests in real headless windows, and generator tests | xUnit v3, Avalonia.Headless |

`Avalonia.UI.Reactor.Core` does not reference Avalonia: the API of applications stays our own.

## Repository layout

One type per file; each folder is a namespace.

```
src/Avalonia.UI.Reactor.Core/
  Primitives/      Color, Point, Size, Rect, Thickness, Font, Alignment, KeyGesture and small enumerations
  Theming/         Theme, ThemeFamily, Themes (catalog), Palette, Typography, ColorValue, control themes,
                   ViewEnvironment (inherited theme, font and color)
  Views/           View and its records, Ui (the DSL), Modifiers, ComponentModifiers, ControlTemplates, MenuShortcuts
  State/           Component, [State], Signal, Scope, StateList, Binding
  Rendering/       IDrawingContext and DrawScope (Canvas), CheckboxGlyph
  Runtime/         HotReload
src/Avalonia.UI.Reactor/
  ReactorApp.cs    entry point, WindowOptions, AvaloniaViews (Native)
  Hosting/         ReactorHost (updates, errors, zoom, shortcuts), ReactorApplication
  Reconciliation/  Reconciler, Node, one adapter per view type (TextAdapter, ButtonAdapter…)
  Panels/          FlexStack (stacks), LayerStack (ZStack)
  Menus/           drawn bar (Avalonia Menu) or system bar, synchronization with the views
  Widgets/         ReactorButton, ReactorCheckBox, ReactorTextBox, their look (ReactorThemes), Surface, DrawingCanvas
samples/TodoDemo/
  Program.cs       ReactorApp.Run<TodoApp>(new WindowOptions { … })
  DemoThemes.cs    Standard and Sable families (Sable and Sable Night, with control templates)
  Components/      TodoApp, CounterCard, TodoList
  Models/          Todo, Filter
  Widgets.cs       Card, Segmented, ProgressRing (stateless components)
tests/Avalonia.UI.Reactor.Tests/
  *Tests.cs        one file per topic; Probes/ holds the small test components
```

## Hot reload

[HotReload.cs](../src/Avalonia.UI.Reactor.Core/Runtime/HotReload.cs) is a `MetadataUpdateHandler`: after a code
change applied by `dotnet watch`, Visual Studio or Rider, it re-evaluates every component of every open window, which
keep their state. Already initialized `static readonly` fields (a static theme, for example) are not reloaded, and
changes .NET refuses (a new signature, a renamed class) make `dotnet watch` restart the application.
