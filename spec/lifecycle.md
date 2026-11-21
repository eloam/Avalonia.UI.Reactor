# View lifecycle

- **Status**: Accepted
- **Date**: 2026-11-21
- **Spike**: done (see [Spike results](#spike-results))

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
  A lifecycle is alive when its node was visited, or when the innermost scope that declares it was kept by the update
  without being re-evaluated: the nodes of such a scope are not visited, and the spike shows that without this rule a
  child component would disappear whenever its parent is re-evaluated.
- The actions run **after** the update, posted on the dispatcher: the disappearances first, children before their
  parents, then the appearances and the restarted tasks, parents before their children (ordered by depth in the tree,
  then in tree order). An action that changes state schedules the next update, instead of changing a `Body` being
  computed. A view that goes away before its posted appearance runs gets neither `OnAppear` nor `OnDisappear`.
- When the window closes, the dispatcher may not run again: `OnDisappear` runs at once and the tasks are cancelled.
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
   decoration. **Done.**
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

- **Depends on identity**: a view that the reconciler recreates disappears and appears again. With the identity rules
  (shipped), only an unkeyed view without an identity, shifted by a view of the same type, is recreated.
- **Hot reload**: a running task keeps its old code until it restarts.
- **A forgotten key gives a stale task**: a task without a key does not restart when the state it read changes (SwiftUI
  has the same rule).
- **`Task` is also a type name**: `.Task(…)` may read as a type at first sight (a method does not hide the type, which
  stays usable in the lambda).

## Risks

- **Not verified by the spike**: Windows and Linux (the real window ran on macOS only), a lifecycle inside a control
  template (`RenderTemplate`).
- **Cost**: the sweep visits every lifecycle on every update. The spike measured about +15 % on the updates of a list
  of 1,000 rows that all declare `OnAppear`; the implementation keeps the lifecycles in a set, since removing them from
  a list costs a linear search each.

## Alternatives

| Alternative | Example | Why not |
|---|---|---|
| Hooks | `UseEffect(() => { …; return cleanup; }, [])` | Rules on the order of calls and dependency arrays; our state is `[State]` properties |
| `IDisposable` components | `class TickCounter : Component, IDisposable`, disposed on removal | A component held in a field can be shown again after its removal; only components would get it |
| Avalonia's visual tree events | `AttachedToVisualTree` on the control | Raised again when a control moves into a decoration or within a panel |
| Methods to override | `protected override void OnAppear()` on `Component` | Only for components; could come later, on top of the modifiers |

## Unresolved questions

- Should `.OnChange(value, action)` be added, like SwiftUI's `.onChange(of:)`, or is `.Task(value, …)` enough?
- Should an analyzer suggest a key when a task reads a state before its first `await`?

## Spike results

Done on 2026-11-21 with the .NET SDK 10.0.100, on a copy of the repository: the modifiers, the sweep in the real
reconciler, 11 headless tests (all of the existing ones still pass) and a real window on macOS.

| Check | Result |
|---|---|
| `OnAppear` on an update of the `Body` | Runs once, not again |
| A branch that closes, then opens again | `OnDisappear`, then `OnAppear` again |
| A control moved into a `Surface` and out of it (a background toggled) | Nothing runs; the test checks that the parent is a `Surface` |
| A keyed row removed, rows reordered (`StateList.Move`) | Only the removed row disappears |
| A child component kept while its parent is re-evaluated | Stays alive; without the rule on kept scopes, the test fails |
| The window closes | `OnDisappear` runs at once and the task is cancelled, headless and in a real window |
| A task without a key | Starts once per appearance; an update does not restart it |
| A task with a key | Cancelled and restarted when the key changes |
| Cancellation | `OperationCanceledException` is not reported; another exception is (`ReactorHost.Report`) |
| Continuations after `await` | On the UI thread, headless and in a real window (10 ticks in 2 s with a 200 ms timer) |
| `Task` as the name of a modifier | Compiles with the analyzers; the `Task` type stays usable in the signature and the lambda |
| Cost: 1,000 rows with `OnAppear`, 50 updates | 494–539 ms against 412–465 ms without; removing the rows 47 ms against 45 ms (headless, noisy) |

Lessons from the spike:

- A lifecycle must know the innermost scope that declares it, and the reconciler when a scope was re-evaluated
  (`Scope.EvaluatedPass`): the rule on kept scopes depends on both.
- An appearance is decided during the sweep but runs later: two flags (decided, ran) keep a view that goes away in
  between from running `OnDisappear` without `OnAppear`.
- The `CancellationTokenSource` of a task is disposed when the task ends; only a running task is cancelled.
- `ViewPropsTests` asks for a sample value of each new property type, as intended.

The spike also found an engine bug unrelated to the lifecycle: an unkeyed child at index 0 and a `ForEach` row keyed
`0` take the same place, and the reconciler throws "Two children of the same container have the key '0'". It is fixed:
keyed and unkeyed children have distinct places (test in `ReconcilerTests`).
