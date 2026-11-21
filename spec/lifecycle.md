# View lifecycle

- **Status**: Proposed
- **Date**: 2026-11-21
- **Spike**: not done (see [Risks](#risks))

## Summary

Let a view run code when it appears and when it disappears, and own a task for as long as it is displayed, as SwiftUI
does with `.onAppear`, `.onDisappear` and `.task`. The task is cancelled when the view disappears, so what a view starts
is released deterministically.

```csharp
public sealed partial class TickCounter : Component
{
    [State] private partial int Tick { get; set; }

    public override View Body => Text($"Tick: {Tick}").Task(async cancellation =>
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(cancellation))
            Tick++;
    });
}
```

## Motivation

Nothing tells a component that it leaves the screen: the reconciler only detaches it from its host. A timer, a
subscription or a request started by a view therefore runs until the application ends:

```csharp
public TickCounter() =>
    _timer = new Timer(_ => Tick++, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));   // never stopped
```

This breaks the rule that everything allocated or subscribed is released deterministically. The usual answers are hooks
(`UseEffect(…, [])`, as in React) or `IDisposable` components; SwiftUI's modifiers fit declarative views better (see
[Alternatives](#alternatives)).

## Detailed design

### Modifiers

```csharp
public static T OnAppear<T>(this T view, Action action) where T : View;
public static T OnDisappear<T>(this T view, Action action) where T : View;
public static T Task<T>(this T view, Func<CancellationToken, Task> action) where T : View;
public static T Task<T>(this T view, object? key, Func<CancellationToken, Task> action) where T : View;
```

As every modifier, they exist in `Modifiers` and in `ComponentModifiers`, and their view properties (`AppearAction`,
`DisappearAction`, `TaskAction`, `TaskKey`) are copied by `ViewProps.CopyFrom`.

```csharp
TextField(Bind.Draft).OnAppear(LoadDraft).OnDisappear(SaveDraft)   // once each
Matches().Task(Query, async cancellation =>                        // restarted, the previous search cancelled,
    Results = await SearchAsync(Query, cancellation))              // when Query changes
```

### Appearing and disappearing

A view appears when the reconciler creates its node, and disappears when the node leaves the tree: a branch that
closes, a row removed from a `ForEach`, a component no longer shown, or the window that closes (`ReactorHost` releases
everything when it is detached). The lifecycle follows the node, not the Avalonia control: a control moved into a
decoration (`Surface`) or within a panel is detached from the visual tree and attached again, which must not restart
anything. `NativeHost` had this bug: it released its subscriptions when a background moved it into a surface, and never
took them again (fixed, with a test in `NativeTests`).

- The reconciler already sweeps the scopes that an update did not visit (`LastVisit`); lifecycles use the same sweep.
  Each lifecycle belongs to the scope that declares it, so a scope skipped by an update keeps its lifecycles.
- The actions run **after** the update: an action that changes state schedules the next update, instead of changing a
  `Body` being computed.
- An update keeps the latest closures (`node.View`), as event handlers do: a re-evaluated `Body` never runs `OnAppear`
  again.

### Tasks

- A task starts on the UI thread when its view appears; after `await`, it resumes on the UI thread, so its state changes
  are not concurrent.
- It is cancelled when the view disappears. With a key, it is also cancelled and restarted when the key changes
  (`Equals`), as SwiftUI's `.task(id:)`; several values form a tuple: `.Task((Query, Page), …)`.
- An `OperationCanceledException` after cancellation is expected and ignored. Any other exception is reported as the
  exception of an action is (`ReactorHost.Report`), and the task waits for its key to change or its view to appear
  again.
- Without a key, the task runs once per appearance: a task that reads state when it starts does not see later changes,
  so it needs that state as its key.

### Identity

The lifecycle follows the identity of the view: a view that the reconciler recreates disappears and appears again, and
its task restarts. Today an unkeyed child is matched by its index, so a branch that appears before it recreates it; the
identity rules of [view builder blocks](view-builder.md#identity) fix this and should ship first.

### Diagnostics

| ID | Title | Severity | Example |
|---|---|---|---|
| MVU017 | Task ignores its cancellation token | Warning | `.Task(async _ => await LoadAsync())`: the task keeps running after its view disappears |

The message names the calls to pass the token to. MVU017 follows the identifiers proposed by
[view builder blocks](view-builder.md#diagnostics).

### Delivery order

Each step is its own change, with its tests, and updates the sample app, `docs/` and the README:

1. `OnAppear` and `OnDisappear`, with the sweep of the reconciler. Tests: an action runs once, not again on an update;
   `OnDisappear` runs when a branch closes, a row is removed and the window closes, not when a control moves into a
   decoration.
2. `Task` with and without a key, and MVU017. Tests: cancelled on removal and when the window closes, restarted when the
   key changes, an exception reported (`ReactorTestHost.Errors`).
3. The sample app shows a clock or a search that uses them.

## Strengths

- **Deterministic release**, as the code rules require: a task cannot outlive its view.
- **Declarative**: the view says what it needs while it is displayed; no dependency array, no cleanup lambda, no rule on
  the order of calls.
- **The common case in one line**: `.Task(async c => …)`, `.OnDisappear(SaveDraft)`.
- **Any view**, not only components, with the names of SwiftUI.

## Weaknesses

- **Depends on identity**: until the identity rules of view builder blocks ship, a branch that appears before a view
  restarts its task.
- **Hot reload**: a running task keeps its old code until it restarts.
- **A forgotten key gives a stale task**: a task without a key does not restart when the state it read changes (SwiftUI
  has the same rule).
- **`Task` is also a type name**: `.Task(…)` may read as a type at first sight (a method does not hide the type, which
  stays usable in the lambda).

## Risks

- **Not verified yet**, to try outside the repository on the real reconciler:
  - the sweep sees every removal, including in a scope skipped by an update, and when the window closes;
  - moving a control (into a decoration, within a reordered list) raises nothing;
  - continuations resume on the UI thread, in the application and in headless tests.
- **Cost**: one lifecycle per modified view and a sweep per update, measured with `ReactorTestHost.Stats`.

## Alternatives

| Alternative | Example | Why not |
|---|---|---|
| Hooks | `UseEffect(() => { …; return cleanup; }, [])` | Rules on the order of calls and dependency arrays; our state is `[State]` properties |
| `IDisposable` components | `class TickCounter : Component, IDisposable`, disposed on removal | A component held in a field can be shown again after its removal; only components would get it |
| Avalonia's visual tree events | `AttachedToVisualTree` on the control | Raised again when a control moves into a decoration or within a panel |
| Methods to override | `protected override void OnAppear()` on `Component` | Only for components; could come later, on top of the modifiers |

## Unresolved questions

- In which order do a parent and its children run `OnAppear`, and `OnDisappear`? SwiftUI does not document one.
- Should `.OnChange(value, action)` be added, like SwiftUI's `.onChange(of:)`, or is `.Task(value, …)` enough?
- Should an analyzer suggest a key when a task reads a state before its first `await`?

## Spike results

Not done yet: the risks above are to be verified before the design is accepted.
