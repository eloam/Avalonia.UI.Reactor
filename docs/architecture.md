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

- **Scopes**: a component, a view method called in a block, a `ForEach` item, an `Observe` block, the drawing of a
  `Canvas` and the update of a `Native` view each record the state they read. Only a change to one of those
  re-evaluates them.
- **Threads**: state can change from any thread; the tree and the controls are only touched on the UI thread.
- **Stacks** are our own Avalonia panel (`FlexStack`), which follows SwiftUI's rules.
- **Controls**: `MartletButton`, `MartletCheckBox` and `MartletTextBox` derive from Avalonia's `Button`, `CheckBox` and
  `TextBox`. They set their own structure-only `ControlTheme`; adapters compute their colors for each state from the
  theme. Fluent is only loaded for the window infrastructure, scroll bars and open menus.

| View | Avalonia control |
|---|---|
| `Text` | `TextBlock` |
| `Button` | `MartletButton` (a `Button`) |
| `TextField` | `MartletTextBox` (a `TextBox`) |
| `Checkbox` | `MartletCheckBox` (a `CheckBox`) |
| `MenuBar`, `Menu`, `MenuItem` | `Menu`, `MenuItem`; `NativeMenu` for the macOS system menu bar |
| `VStack`, `HStack` | `FlexStack` (a `Panel`) |
| `ZStack`, `.Overlay` | `LayerStack` (a `Panel`) |
| `Divider` | `Border` |
| `Box`, decorations, `.OnTap` | `Surface` (a `Border`) |
| `Canvas` | `DrawingCanvas` (a `Control` drawing with Avalonia's `DrawingContext`) |
| `Native(…)` | the control you provide |

## What happens when a state changes

Each piece of state (a `[State]` property, a `StateList`) has a list of subscribers, like a sign-up sheet.

1. **Computing while recording.** The reconciler evaluates a scope with `Scope.Evaluate`: while its `Body` runs, every
   state it reads adds the scope to its sheet (`Signal.Read`). The sheets of the previous evaluation are cleared first,
   so that a branch of an `if` that is no longer shown stops counting.
2. **Changing a state.** The generated setter compares the new value with the old one; when it differs, it marks every
   subscriber of its sheet for re-evaluation (`Signal.Notify`) and asks for an update, from any thread.
3. **Updating.** On the UI thread, the reconciler walks the tree. A scope that is not marked keeps its controls as they
   are; a marked scope runs its `Body` again, and each resulting view is applied to the control that held its place.

In the sample app, clicking **+** marks the counter card only, since it is the only `Body` on the sheet of `Count`.

The reconciler ([Reconciler.cs](../src/MartletUI/Reconciliation/Reconciler.cs)) walks the views and the
scopes, and delegates the rest:

| Class | Role |
|---|---|
| `ChildSlots` | The place of each child of a container: its key, its identity, or its type and rank |
| `ViewAdapter` and its subclasses | One per type of view: create the Avalonia control, set its properties |
| `Decorations` | The background, border, corners, padding and click of a view whose control does not draw them |
| `Placement` | The size and flexibility of a control, which its container reads |
| `LifecycleTracker` | `OnAppear`, `OnDisappear` and `Task`, which follow the nodes, after each update |
| `ControlTree` | Moves controls between parents without recreating them |

## Projects

| Project | Role | Depends on |
|---|---|---|
| `MartletUI.Core` | The API of applications: views, DSL, modifiers, themes, reactive state, scopes, hot reload | nothing |
| `MartletUI` | `MartletApp`, `MartletHost`, the reconciler, panels, menus, our controls and their look | Avalonia 12 |
| `MartletUI.Generators` | Source generators of `[State]` and of view builder blocks, and the diagnostics analyzers, run at compile time | Roslyn |
| `MartletUI.CodeFixes` | Code fixes and refactoring of view builder blocks, run by the IDE (a separate assembly: analyzers must not reference the workspaces) | Roslyn workspaces |
| `samples/TodoDemo` | Sample app: counter, task list, async action, theme picker, menu bar | — |
| `tests/MartletUI.Tests` | Tests in real headless windows, and generator tests | xUnit v3, Avalonia.Headless |

`MartletUI.Core` does not reference Avalonia: the API of applications stays our own.

## Repository layout

One type per file; each folder is a namespace.

```
src/MartletUI.Core/
  Primitives/      Color, Point, Size, Rect, Thickness, Font, Alignment, KeyGesture and small enumerations
  Theming/         Theme, ThemeFamily, Themes (catalog), Palette, Typography, ColorValue, control themes,
                   ViewEnvironment (inherited theme, font and color)
  Views/           View and its records, Ui (the DSL), Modifiers, ComponentModifiers, ControlTemplates, MenuShortcuts
  State/           Component, [State], Signal, Scope, StateList, Binding
  Rendering/       IDrawingContext and DrawScope (Canvas), CheckboxGlyph
  Runtime/         HotReload, ViewCollector (the views of view builder blocks)
src/MartletUI/
  MartletApp.cs    entry point, WindowOptions, AvaloniaViews (Native)
  Hosting/         MartletHost (updates, errors, zoom, shortcuts), MartletApplication
  Reconciliation/  Reconciler, Node, ChildSlots, LifecycleTracker, Decorations, Placement, ControlTree, one adapter
                   per view type (TextAdapter, ButtonAdapter…)
  Panels/          FlexStack (stacks), LayerStack (ZStack)
  Menus/           drawn bar (Avalonia Menu) or system bar, synchronization with the views
  Widgets/         MartletButton, MartletCheckBox, MartletTextBox, their look (MartletThemes), Surface, DrawingCanvas
samples/TodoDemo/
  Program.cs       MartletApp.Run<TodoApp>(new WindowOptions { … })
  DemoThemes.cs    Standard and Sable families (Sable and Sable Night, with control templates)
  Components/      TodoApp, CounterCard, TodoList
  Models/          Todo, Filter
  Widgets.cs       Card, Segmented, ProgressRing (stateless components)
tests/MartletUI.Tests/
  *Tests.cs        one file per topic; Probes/ holds the small test components
```

## Hot reload

[HotReload.cs](../src/MartletUI.Core/Runtime/HotReload.cs) is a `MetadataUpdateHandler`: after a code
change applied by `dotnet watch`, Visual Studio or Rider, it re-evaluates every component of every open window, which
keep their state. Already initialized `static readonly` fields (a static theme, for example) are not reloaded, and
changes .NET refuses (a new signature, a renamed class) make `dotnet watch` restart the application.
