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
| `Group(…)` | Several views where one is expected; unfolded in place inside a stack |
| `ForEach(items, row, key)` | One view per item, mixed freely with the other children |
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

Modifiers also apply to components: `counter.Padding(8).Background(ColorRole.Surface)`. Named arguments remove any
ambiguity: `.Padding(horizontal: 6, vertical: 4)`. A border takes its own space, as in CSS: with
`.Padding(16).Border(…, 2)`, the content starts at 18.

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
