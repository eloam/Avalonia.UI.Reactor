# Reactive state

A view that **reads** a piece of state subscribes to it; **changing** that state re-evaluates only the views that read
it. This is the model of `$state` in Svelte, `@State` / `@Observable` in SwiftUI and `mutableStateOf` in Jetpack Compose.

## Declaring state

| Code | Meaning |
|---|---|
| `[State] partial int Count { get; set; }` | Observed property. An initial value is allowed: `{ get; set; } = "";`. The class must be `partial`. |
| `StateList<Todo> Todos { get; } = [a, b];` | Observed list: `Add`, `Remove`, `RemoveAll`, `Move`… re-evaluate whatever iterates it. |
| `[State]` on a model class (`Todo.Done`) | Checking a task re-evaluates its row only, plus whatever counts the tasks. |
| `VStack(counter, todoList)` | A component is used like a view. It keeps its state and is re-evaluated only when needed. |
| `ForEach(todos, Row, t => t.Id)` | One scope per item; the key keeps each row's controls, focus and hover. |
| `TextField(Bind.Draft)`, `Checkbox(todo.Bind.Done)` | Two-way binding, generated for every settable `[State]` property. |
| `Observe(() => …)` | Isolates part of a `Body` in its own scope without creating a component. |

```csharp
public sealed partial class TodoList : Component
{
    [State] partial string Draft { get; set; } = "";
    StateList<Todo> Todos { get; } = [];

    public override View Body =>
        VStack(
            HStack(
                TextField(Bind.Draft, "What needs to be done?").Grow(),
                Button("Add", Add)),
            ForEach(Todos, todo => Checkbox(todo.Bind.Done, todo.Title), todo => todo.Id));

    void Add()
    {
        Todos.Add(new Todo(Draft));
        Draft = "";
    }
}

public sealed partial class Todo(string title)
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; } = title;
    [State] public partial bool Done { get; set; }
}
```

## Rules

- State can change **from anywhere**: an action, the continuation of an `await`, a timer or a background thread.
  The update is scheduled on the UI thread; no `Refresh()` call is needed.
- A `Body` is **pure**: it reads state, it never changes it and performs no I/O.
- A scope whose state did not change keeps its controls as they are: its `Body` is not called again and its controls
  are not touched.
- Plain fields are **not** observed. Use `[State]`, or call `Refresh()` as a last resort.
- A component keeps its state as long as the same instance is displayed: create it once, in a field
  (`private readonly Counter _counter = new();`), not in `Body` (MVU016).

In the sample app, clicking **+** re-evaluates one scope (the counter), typing re-evaluates one (the input row), and
checking a task re-evaluates two (the row and the list footer).

## The generator

The property code is written at compile time by a Roslyn source generator
([StateGenerator.cs](../src/Avalonia.UI.Reactor.Generators/StateGenerator.cs)), the equivalent of the Svelte compiler.
It also writes the `Bind` property that exposes the bindings; it stays private when the state properties are private.

## Diagnostics

An analyzer ([StateAnalyzer.cs](../src/Avalonia.UI.Reactor.Generators/StateAnalyzer.cs)) reports mistakes as you type.

| ID | Severity | Description |
|---|---|---|
| `MVU001` | Error | `[State]` on a property that is not `partial` |
| `MVU002` | Error | `[State]` in a class that is not `partial` |
| `MVU003` | Error | `[State]` on a property with a body, a `static` property, or a property without `get` |
| `MVU004` | Error | `Bind` or `StateBindings` declared by hand in a class that has state: these names are reserved for the generated bindings |
| `MVU010` | Warning | A field displayed by the view and modified, but not `[State]`: the screen would not follow |
| `MVU011` | Warning | A `List<T>` displayed by the view and modified: use `StateList<T>` |
| `MVU012` | Error | State passed to a control without its binding (`TextField(Name)`, `Checkbox(todo.Done)`): the message gives `Bind.Name`, `todo.Bind.Done` |
| `MVU013` | Error | A view created as a statement, whose value is not used (`Text("Title");`): it would never be displayed |
| `MVU016` | Error | A component with state created in a view (`VStack(new Counter())`): a new instance on every update loses its state |
| `MVU017` | Warning | A task given to `.Task(…)` that ignores its cancellation token: it would keep running after its view disappears |
