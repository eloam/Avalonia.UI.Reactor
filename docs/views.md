# Writing views

Views are immutable records created by the factory functions of `Ui` and refined with chainable modifiers. Import the
factories once with `global using static Avalonia.UI.Reactor.Views.Ui;`.

## Building blocks

| View | Purpose |
|---|---|
| `Text("…")` | A single line of text; a line too long for its place ends with an ellipsis (`…`) |
| `Button("OK", action)` | A push button; the action can be `Action` or `Func<Task>` |
| `TextField(Bind.Name, "Placeholder", onSubmit)` | A single-line text field (selection, undo, input methods, context menu) |
| `Checkbox(Bind.Done, "Label")` | A checkbox with an optional label |
| `VStack(…)`, `HStack(…)` | Vertical and horizontal stacks |
| `ZStack(…)` | Layers; the last child is on top and receives the clicks |
| `Group(…)` | Several views where one is expected; unfolded in place inside a stack, its modifiers applied to each child |
| `ForEach(items, row)` | One view per item, mixed freely with the other children (see [Lists](#lists)) |
| `Spacer()`, `Divider()`, `Box()` | Flexible space, separator line, empty decorated surface |
| `Canvas(d => …)` | Free drawing |
| `Native(() => new Calendar())` | Any Avalonia control |

## Stacks

Children come first; spacing and alignment are modifiers. A stack **stretches on its own** when one of its children
stretches (`Spacer()`, `.Grow()`, `.FillWidth()`), so there is no need to write `.FillWidth()` on the stack itself.

```csharp
VStack(Header(), ForEach(todos, Row), Footer())   // ForEach mixes with the other children
    .Spacing(16)                                  // otherwise, the spacing of the theme
HStack(Text("Tasks"), Spacer(), Badge())          // the spacer stretches the row, the badge goes right
ZStack(Icon(), Badge()).Alignment(Alignment.TopRight)
```

The rules are SwiftUI's: children take their natural size, `Spacer` and `.Grow(n)` share the remaining space in
proportion, and an explicit `.Width(…)` wins over flexible children.

## Lists

`ForEach` builds one view per item, each re-evaluated on its own. A row keeps its controls, focus and hover when the
list changes order, thanks to its key, chosen in this order:

1. the key function, when one is given: `ForEach(todos, Row, t => t.Title)`;
2. the identifier of an item that implements `IIdentifiable<TId>`, like `Identifiable` in SwiftUI;
3. otherwise the item itself: its value for a number or a string, its instance for an object.

```csharp
public sealed record Contact(int Id, string Name) : IIdentifiable<int>;

ForEach(contacts, c => Text(c.Name))   // keyed by c.Id, even when the records are rebuilt on every update
```

## Modifiers

| Modifier | Effect |
|---|---|
| `.Padding(16)`, `.Padding(horizontal: 6, vertical: 4)` | Inner spacing |
| `.Width(…)`, `.Height(…)`, `.Size(w, h)` | Fixed size |
| `.FillWidth()`, `.FillHeight()`, `.Grow(n)`, `.AlignSelf(…)` | Behavior inside a stack |
| `.Background(…)`, `.Border(color, width)`, `.Radius(…)` | Decoration |
| `.Foreground(…)`, `.Font(TextStyle.Title)`, `.Font("caption")`, `.FontSize(…)`, `.Bold()`, `.TextAlignment(…)` | Text |
| `.Theme(…)`, `.ColorScheme(…)` | Theme of a branch (see [Theming](theming.md)) |
| `.OnTap(() => …)` | Makes any view clickable, with the hover overlay of the theme |
| `.Overlay(badge, Alignment.TopRight)` | Places a view over another |
| `.Key("…")` | Identity of a view in a list: keeps its control, focus and hover |
| `.OnAppear(…)`, `.OnDisappear(…)` | Runs an action when the view enters the tree and when it leaves it (see [Lifecycle](#lifecycle)) |
| `.Task(async cancellation => …)`, `.Task(key, …)` | Runs a task while the view is displayed, cancelled when it disappears |

Modifiers also apply to components: `counter.Padding(8).Background(ColorRole.Surface)`. Named arguments remove any
ambiguity: `.Padding(horizontal: 6, vertical: 4)`. A border takes its own space, as in CSS: with
`.Padding(16).Border(…, 2)`, the content starts at 18.

A modifier on a `Group` applies to **each** of its children, as in SwiftUI, and the children stay in place:
`HStack(Group(a, b).Padding(8))` pads `a` and `b`, side by side. A child keeps the modifiers it sets itself. To treat
several views as one (a single background, a single padding), use a `VStack` or a `Box`. `.OnAppear` and `.Task` on a
group also run once per child. The key of a group identifies it: it is placed above the keys of its children.

## Lifecycle

A view can run an action when it appears and when it disappears, as with `.onAppear` and `.onDisappear` in SwiftUI:

```csharp
TextField(Bind.Draft).OnAppear(LoadDraft).OnDisappear(SaveDraft)
```

- The view **appears** when it enters the tree, and **disappears** when it leaves it: a branch that closes, a row
  removed from a list, a component no longer shown, or the window that closes. An update of the `Body` does not run
  `OnAppear` again, and moving the control (into a decoration, within a reordered list) does not count.
- The actions run **after** the update, parents before their children for `OnAppear`, children before their parents
  for `OnDisappear`. A state they change schedules the next update. When the window closes, `OnDisappear` runs at once.
- An exception in an action is reported like the exception of any other action.

A view can also own a **task** for as long as it is displayed, as with `.task` in SwiftUI: the task is cancelled
when the view disappears, so nothing it started keeps running.

```csharp
Text($"Open for {Seconds} s").Task(async cancellation =>
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
    while (await timer.WaitForNextTickAsync(cancellation))
        Seconds++;
})

Results().Task(Query, async cancellation => Matches = await SearchAsync(Query, cancellation))
```

- The task starts after the update, once per appearance, on the user interface thread; after an `await`, it resumes
  there. An update of the `Body` does not restart it.
- With a key, the task is cancelled and started again when the key changes (`Equals`); several values form a tuple:
  `.Task((Query, Page), …)`. A task that reads state when it starts takes that state as its key.
- Cancellation goes through the token: pass it to what the task awaits (MVU017 warns otherwise). An
  `OperationCanceledException` after cancellation is expected; another exception is reported.

## Actions and bindings

```csharp
Button("+", () => Count++)
TextField(Bind.Draft, "Name")              // reads and writes Draft
Checkbox(todo.Bind.Done)                   // reads and writes todo.Done
TextField(value, v => Value = v.Trim())    // computed value: explicit getter and setter
```

Text fields and checkboxes are **controlled**: input goes to the state, then the control shows the value of its view.
State that rejects or transforms the input has the last word.

## Asynchronous code

An action can be `async Task`. The interface updates before and after each `await`, with nothing to add.
Work that must stop when a view goes away (a timer, a subscription, a search as you type) belongs in `.Task(…)` (see
[Lifecycle](#lifecycle)).

```csharp
Button("Load", Load)

async Task Load()
{
    Loading = true;
    Items = await api.GetItemsAsync();
    Loading = false;
}
```

## Errors

An exception thrown by an action — synchronous, `async Task` or even `async void` — does not close the application.
It is written to the standard error output and to the debugger, prefixed with `[Reactor]`.

## Components and conditional views

- A component with state is a class deriving from `Component`; a stateless one is just a function that returns a
  `View` (see `Card` and `Segmented` in the sample app).
- Conditional display: `condition ? Text("…") : null`; several views at once: `condition ? Group(a, b) : null`.
- Several views returned by a component, a `ForEach` row or an `Observe` block take their places among the children of
  the container, as in SwiftUI: in an `HStack`, `ForEach(items, item => Group(Name(item), Badge(item)))` puts every
  view side by side. A block re-evaluated alone updates its own views only; the modifiers of a component or a block
  whose content is several views apply to each of them. In a place for one control (the root of a window, a
  decoration), the views are stacked vertically.

## Layers and drawing

```csharp
// Layers: the last child is on top
ZStack(Avatar(), Text("JD"))
Icon().Overlay(Badge(3), Alignment.TopRight)

// Drawing: fixed colors or theme roles; the area is d.Bounds
Canvas(d =>
{
    d.StrokeCircle(d.Bounds.Center, 18, ColorRole.Border, 4);
    d.Arc(d.Bounds.Center, 18, -90, 360 * progress, ColorRole.Accent, 4);
}).Size(40, 40)
```

`DrawScope` provides rectangles, circles, arcs, lines, polygons and text; `d.Context` gives access to the drawing
context for the rest (shadows, clipping). A canvas tracks the state it reads **while drawing** and redraws on its own
when that state changes. The `ProgressRing` of the sample app shows that a custom control is just a function.

## Embedding an Avalonia control

Any Avalonia control without an equivalent in the DSL can be embedded as is. It is the only API that exposes an
Avalonia type.

```csharp
using static Avalonia.UI.Reactor.AvaloniaViews;

Native(() => new Calendar(), calendar => calendar.DisplayDate = Today)   // created once, updated from the state
```

The update tracks the state it reads: it runs again on its own when that state changes.
