# View builder blocks

- **Status**: Proposed
- **Date**: 2026-11-21
- **Spike**: partial (see [Spike results](#spike-results))

## Summary

Allow `if`, `else` and `switch` statements inside views, as SwiftUI does with `@ViewBuilder`. A view written as a
statement in a *view builder block* becomes a child of the enclosing container, and `Show(…)` adds any other view
expression. Lists go through a `ForEach` block, and a view method called as a statement of a block gets its own scope,
like a SwiftUI subview. Before blocks, the reconciler learns to keep identity across branches, to apply group modifiers
to each child and to insert the views of a scope into its parent (see [Delivery order](#delivery-order)).

```csharp
public override View Body => VStack(() =>
{
    Header();
    if (IsLoggedIn)
    {
        Text($"Hello, {Name}!").Bold();
    }
    else
    {
        Button("Sign in", SignIn);
    }

    ForEach(Todos, todo =>
    {
        Row(todo);
        if (todo.IsLate)
        {
            Text("Late").Foreground(ColorRole.Danger);
        }
    });
});
```

## Motivation

Today a condition is written with `? :` and `null`, several views with `Group`, and anything longer moves to a method:

```csharp
VStack(
    Header(),
    IsLoggedIn ? Text($"Hello, {Name}!").Bold() : Button("Sign in", SignIn),
    ShowDetails ? Group(Text("Size: 42"), Text("Color: blue")) : null)
```

This is concise for one condition, but nested conditions, rows with conditional content and local variables quickly
become hard to read. SwiftUI and Jetpack Compose let the view use the control flow of the language; this proposal brings
the same to C#.

## Detailed design

### Builder parameters

A container marks the parameter that receives a block with `[ViewBuilder]`, next to today's overload:

```csharp
public static StackView VStack(params View?[] children);           // VStack(a, flag ? b : c): children, as today
public static StackView VStack([ViewBuilder] Action content);       // VStack(() => { … }): a builder block
```

A lambda without braces is a block of one statement: `VStack(() => Text("a"))` picks `Action`, and its view is
intercepted like any other. A conditional is not a statement, so it stays an argument: `VStack(flag ? a : b)`. A
`Func<…>` overload is kept only where arguments cannot express it: the rows of `ForEach` and `Observe`
(`todo => todo.Done ? a : b`).

The two ways to write children are an assumed exception to the "no duplicated API" rule, with one usage rule:
arguments for a fixed list of views (`VStack(a, b)`, on one line), a block as soon as there is an `if`, a `switch` or
a local variable. A code fix "Convert to block" is offered on a `? :` or a `null` among the arguments.

Applications mark their own parameters the same way. A reusable piece of view is a method that returns a group built by
a block, the equivalent of SwiftUI's `@ViewBuilder var details: some View`:

```csharp
public static StackView Card([ViewBuilder] Action content) => VStack(content).Padding(16).Radius(10);

private View Details() => Group(() =>
{
    Text("Size: 42");
    if (HasColor)
    {
        Text("Color: blue");
    }
});

Details();                // in a block: its own scope, whose views join the block (see Scopes and Fragments)
Details().Padding(8);     // a view like any other: modified, stored, passed as an argument, tested
```

### Lists

As in SwiftUI, a list is a `ForEach` view whose row is itself a block. It gets a block overload next to today's one:

```csharp
public static GroupView ForEach<T>(IEnumerable<T> items, Func<T, View> row, Func<T, object>? key = null);             // today
public static GroupView ForEach<T>(IEnumerable<T> items, [ViewBuilder] Action<T> row, Func<T, object>? key = null);   // block
```

```csharp
ForEach(Todos, todo => Row(todo))                                       // Func<T, View>: one view per row
ForEach(Todos, todo => { Row(todo); if (todo.IsLate) { Badge(); } })    // Action<T>: a block per row
```

Each row keeps its own scope and key: checking a task re-evaluates its row only, and a reordered list keeps the
controls, focus and hover of its rows. A loop would lose both, so loops are rejected in a block (see
[Statements](#statements)).

**Keys.** An object is its own key by default, but a `record` rebuilt on every update needs one, and the key argument
comes after the block. As SwiftUI reads the key from `Identifiable`, an item that implements `IIdentifiable<TId>` gives
its own key:

```csharp
public sealed partial class Todo : IIdentifiable<Guid>
{
    public Guid Id { get; } = Guid.NewGuid();
}

ForEach(Todos, todo => { Row(todo); })   // keyed by todo.Id
```

`IIdentifiable<TId>` derives from a non-generic `IIdentifiable` and implements its key by default
(`object IIdentifiable.Key => Id`), so `ForEach` reads it without reflection, compatible with AOT. Keys are useful
without blocks (`ForEach(Todos, Row)`), so they ship first.

**Bindings.** SwiftUI also gives each row a binding to its item
(`ForEach($tags) { $tag in TextField("Tag", text: $tag) }`). `StateList` has no such bindings today, so a row edits its
item through an index. A list of bindings does the same:

```csharp
ForEach(Tags.Bindings, tag => TextField(tag))   // tag: Binding<string>
```

A binding is a new object on every update, so these rows are keyed by their index unless a key is given. An overload of
`ForEach` on `StateList<T>` would be ambiguous with the one that receives the item.

### Identity

SwiftUI gives each branch of an `if` its own identity (a `_ConditionalContent` in the type of the view), so the views
that follow keep theirs. Our reconciler matches an unkeyed child by its index: a branch that appears shifts the
following children, which are recreated and lose focus and caret.

```csharp
VStack(() =>
{
    if (HasError)
        Text(Error);

    TextField(Bind.Email);   // index 0, then 1: today a new TextBox, and the focus is lost
});
```

The generator cannot rewrite the `if`, and an identity per call site, as in Compose, would need one interceptor per
call, named after its rank, which breaks hot reload (see [Lowering](#lowering)). The reconciler therefore identifies a
child, in this order, by:

1. its `Key`;
2. an identity it already has: a component by its instance, a bound control by the property of its binding
   (`TextField(Bind.Email)` by the `Email` property of its owner);
3. otherwise, its **view type and its rank among the children of that type** ("the first `Text`"): such views have
   little state to lose.

```csharp
if (IsCompany)
    TextField(Bind.Company);

TextField(Bind.Email);   // identified by its binding: keeps its control and focus when Company appears
```

`Binding<T>` does not know its target today (it holds two delegates): the generator gives it its owner and the name of
the property. Two controls bound to the same property fall back to rule 3. The scope of a view method is identified by
its method: in `if (Compact) { Summary(); } else { Progress(); }`, the two calls are different children, although
neither has an argument whose change would tell them apart.

The problem already exists with `cond ? view : null` (`null` children are removed), so these rules ship before blocks.
They also change how existing views are reconciled (controls are moved instead of recreated), so the reconciler tests
are checked with them.

A scope inserted into its parent (see [Fragments](#fragments)) is itself a child identified this way, and its views are
identified within it: a branch that appears in `Details()` does not shift the views that follow `Details()`.

Identity also prepares transitions: in SwiftUI, `if showBanner { Banner().transition(.move(edge: .top)) }` animates the
branch in and out. Animations are a separate proposal; the reconciler will then keep a removed control until its exit
animation ends.

### Groups

Today a `Group` that has modifiers becomes a `VStack`: in an `HStack`, `Details().Padding(8)` would stack its views
vertically. As in SwiftUI, a modifier on a `Group` applies to each child, which the parent still lays out; a `VStack`
or a `Box` treats several views as one. `Key` is the exception: it identifies the group itself, since the same key on
each child would be a duplicate key.

This is a breaking change: today's behavior is tested (`A_group_with_modifiers_stacks_its_children_vertically`) and
described in `docs/views.md`; both change with it.

```csharp
HStack(() => { Details().Padding(8); })   // each view of Details gets its own padding, side by side
```

### Scopes

A scope records the state read while it runs, and a view is a record built from values already read: `Show` changes
nothing, only the place of the read does. In SwiftUI, a view extracted into its own type is re-evaluated only when its
own inputs change. Here, only a component, a `ForEach` row and `Observe` have their own scope; a method is plain code
that runs again with the whole `Body`.

As the Compose compiler does for each `@Composable` function, the interceptor of a statement that calls a view method
wraps the call in a scope instead of running it at once. A view method is any method that returns a view, in the
application or in a component library, except the factories and modifiers of `Avalonia.UI.Reactor.Core`:

```csharp
private View Progress() => Group(() =>                  // its own scope: re-evaluated alone when Done or Total changes
{
    Text($"{Done}/{Total}");
    if (Done == Total)
        Text("All done");
});

VStack(() => { Header(); Progress(); TodoRows(); });    // no Observe needed
```

- Only a statement of a block that starts with a view method gets a scope: `Progress();` and `Progress().Padding(8);`
  both do, so adding a modifier does not change what is re-evaluated. The modifiers apply to the scope, as
  `ScopeView.ModifierWrapper` does for a component, and to each of its views, as for a `Group`. `Progress()` passed as
  an argument runs at once.
- `Refresh()`, the escape hatch that MVU010 suggests for a plain field, re-evaluates the component and also marks the
  scopes of its view methods: they do not read the refresh signal, so they would otherwise be skipped.
- The caller reads the arguments; what the method reads belongs to its scope. When the caller is re-evaluated, the
  scope is skipped if its arguments are equal, as SwiftUI compares the inputs of a view (today an anonymous scope is
  re-evaluated with its parent).
- The method runs when the reconciler applies its scope, not during `Body`: the call stack shows the reconciler between
  the two, an exception surfaces then, and stepping into the call does not enter the method (a breakpoint in it does).

For a block written in place, `Observe` gets a block overload:

```csharp
Observe(() => { Text($"{Done}/{Total}"); if (Done == Total) { Text("All done"); } });
```

### Fragments

A scope produces a single control today: when its content is several views (a view method, a `ForEach` row block, an
`Observe` block), they form a `Group`, which becomes a `VStack`, even in an `HStack`. As SwiftUI does with the views of
a `ForEach` row, and React with fragments, the reconciler inserts the views of a scope directly into the parent
container.

- A fragment owns a contiguous range of its parent's controls: when its scope is re-evaluated alone, the reconciler
  replaces that range only, and the controls around it stay.
- A container that holds a single child (`Box`, a decoration, a button label, a template) and the root of a component
  cannot hold several controls: there, the views of a fragment are stacked vertically, as a `Group` is today.

### Show

```csharp
public static void Show(View? view);
```

Swift accepts any view expression as a statement; C# only accepts calls, assignments and `new` (CS0201). `Show` adds the
result of any expression to the block: a variable, a component, a conditional or a `switch` expression. A `null` view
adds nothing, as a `null` child does today. Outside a block, `Show` throws `InvalidOperationException`, and MVU013
reports the call at compile time.

```csharp
Show(_counter);                                              // a component
Show(IsLoggedIn ? Text("Hi") : Button("Sign in", SignIn));   // a conditional: CS0201 without Show
Show(Tab switch                                              // a switch expression
{
    Tab.Home => Home(),
    Tab.Settings => Settings(),
});
```

In a block, a code fix offers "Wrap in 'Show'" on CS0201.

A member of the class hides a function imported by `using static Ui`: an `Add()` action, as in the sample app, would
hide an `Add(view)`. The name must therefore be rare; `Show` clashes with nothing in the sample app, although it may
recall opening a window (`Window.Show()`).

### Lowering

C# has no result builders, and a source generator cannot rewrite user code. The design uses **interceptors**: the
generator replaces each view statement of a block with a call that creates the view as written, then emits it into the
block being collected.

```csharp
// The user writes:              // The generator emits (simplified):
Text("Size: 42");                [InterceptsLocation(1, "…")]
                                 public static TextView View_Ui_Text_string(string content)
                                     => ViewCollector.Emit(Ui.Text(content));

Progress();                      [InterceptsLocation(1, "…")]
                                 public static View View_Page_Progress(Page page)
                                     => ViewCollector.EmitScope(s_progress, () => CallProgress(page));
```

- `VStack(Action)` runs the block inside `ViewCollector.Collect`, which keeps the current block per thread.
- A statement that calls a view method emits a scope that runs the method later, identified by the method
  (`s_progress`; see [Scopes](#scopes)).
- **One interceptor per intercepted method**, carrying one `[InterceptsLocation]` per call, with a name derived from the
  method: adding, moving or removing a call never renames an interceptor (hot reload refuses renames).
- Private methods of the user's type are called through `[UnsafeAccessor]`: no reflection, compatible with AOT.
- Interceptors are marked `[DebuggerStepThrough]`.
- Interceptors require an opt-in: the package sets `InterceptorsNamespaces` in its `buildTransitive` targets.
- A block compiled without the generator would be empty, without an error. The generator registers each assembly it
  processes (a module initializer), and `VStack(Action)` throws when the assembly of the block (`content.Method.Module`)
  is not registered.

### Statements

A block accepts, in C# form, what SwiftUI accepts in a `@ViewBuilder` (`if`, `switch`, `let`, `ForEach`); MVU014
rejects the rest:

| Accepted | Rejected by MVU014 |
|---|---|
| Calls that return a view, `Show(…)`, and calls to `[Conditional]` methods (`Debug.WriteLine`) | Other expressions: `Count++;`, `Log();` (returns no view), `new Card(…);` |
| `if` and `switch`; `if (User is { } user)` plays the part of `if let` | Loops (`for`, `foreach`, `while`, `do`): a code fix converts `foreach` into `ForEach` |
| Local variables; `return`, `break` (in a `switch`), `throw` | `try`, `using`, `lock`, `goto`, `await`, local functions |

`return` goes beyond SwiftUI, but an early exit (`if (Todos.Count == 0) { Text("Empty"); return; }`) is only another
way to write `if` / `else`. A discard (`_ = …;`) is the explicit escape hatch for an expression that returns a value,
like `let _ =` in Swift. A method that returns nothing cannot be discarded, so calls to `[Conditional]` methods are
accepted: `Debug.WriteLine` traces a value while debugging and vanishes in Release. An `async` lambda
(`VStack(async () => …)`) would be `async void`: MVU014 rejects it.

A `switch` statement needs a `break` per case and does not check that every case is handled: a value added to an enum
later shows nothing. Swift's `switch` is exhaustive. A `switch` expression through `Show` is shorter and checked by the
compiler (CS8509); a `switch` statement still fits a case that shows several views, and MVU015 reports the enum values
it misses.

### Code style

The examples follow the repository style (`.editorconfig`, checked with `dotnet format`). A block written on one line
stays on one line, even with an `if` (`ForEach(Todos, todo => { Row(todo); if (todo.IsLate) { Badge(); } })`). In a
block that spans several lines, an `if` takes its own lines, and a single view needs no braces (see the examples in
[Identity](#identity)). SwiftUI writes the condition on one line (`if hasError { Text(error) }`): this is the price of
the style, not of the design. Settings come after the block (`.Spacing(8)`), so a long block is better split into view
methods, which cost nothing now that they have their own scope (see [Scopes](#scopes)).

### Diagnostics

| ID | Title | Severity | Example |
|---|---|---|---|
| MVU013 | View is created but never displayed | Error | `Text("Title");` or `Show(x);` outside a block |
| MVU014 | Statement in a view block does not produce a view | Error | `Count++;`, `foreach` or an `async` lambda in a block |
| MVU015 | Switch in a view block does not handle every value | Warning | `switch (Tab)` without `case Tab.Settings` or `default` |
| MVU016 | Component is created in a view | Error | `Show(new CounterCard())` or `VStack(new CounterCard())`, when `CounterCard` declares state |

A component created in a view is a new instance on every update, so its state is lost each time: MVU016 asks to keep it
in a field or, for a list row (`ForEach(Todos, t => Show(new TodoRow(t)))`), to move its state into the item
(`todo.Bind.Done`) and draw the row with a view method. MVU013 and MVU016 are useful without blocks
(`VStack(new CounterCard())` loses its state today); they are implemented, and MVU013 reports every view statement
until blocks exist.

### Delivery order

Each step is its own change, with its tests, and updates the sample app, `docs/` and the README:

1. MVU013, MVU016 and `IIdentifiable` keys, useful today. **Done.**
2. Identity by components, bindings, then view type and rank, which fixes `cond ? view : null` today (a behavior
   change). **Done.**
3. Group modifiers applied to each child: a breaking change (`refactor!`).
4. Fragments.
5. Blocks: `[ViewBuilder]` parameters, interceptors, `Show`, MVU014, MVU015 and the code fixes.
6. Scopes of view methods and the `Observe` block, measured (see [Risks](#risks)).
7. `Tags.Bindings`.

## Strengths

- **Real control flow**: `if`, `switch` and local variables, without `? :` chains or helper methods.
- **Lists as in SwiftUI**: a `ForEach` block per row, which keeps per-row re-evaluation and identity.
- **One attribute**: `[ViewBuilder]` only marks parameters; a reusable piece of view is a method that returns a view,
  which can be modified, stored, passed and tested.
- **Extracting a method isolates it**: a view method is re-evaluated on its own, like a SwiftUI subview, without
  `Observe`.
- **The user's code runs as written**: breakpoints hit the block, and the call stack shows its lines (a scoped method is
  called from the reconciler).
- **Hot reload works**: every edit tested in the spike is applied without restarting (see below).
- **Fewer silent failures**: MVU013, MVU014 and MVU016 turn a lost view, a stray statement or a lost state into a
  compile error, and a block compiled without the generator throws. A stale plain field remains silent (see
  [Weaknesses](#weaknesses)).
- **Purity checked at compile time**: MVU014 rejects state changes (`Count++`) in a view block.
- **Opt-in by syntax**: the existing `VStack(a, b)` style stays; blocks are used only where control flow helps.
- **Fewer limits than SwiftUI**: no maximum number of children (SwiftUI had ten until Swift 5.9), no "unable to
  type-check this expression in reasonable time" error, and pattern matching in `if` and `switch`.
- **Platform conditions as in Swift**: `if (OperatingSystem.IsMacOS()) { … }` plays the part of `#if os(macOS)` and
  `if #available`, and the CA1416 analyzer checks the platform-specific calls it guards.

## Weaknesses

- **Two ways to write children** (arguments and blocks): more API surface, an assumed exception to the "no duplicated
  API" rule, with a usage rule and a code fix (see [Builder parameters](#builder-parameters)).
- **The same call has two meanings**: in an expression, `Text("a")` creates a view; as a statement of a block, it
  creates and adds it. `Progress()` runs at once as an argument, but later in its own scope as a statement. Nothing in
  the code shows it ("Go to definition" leads to `Ui.Text`, not to the interceptor); Swift does the same, but the
  language says so. MVU013 catches the case where the view would be lost.
- **No trailing closures**: SwiftUI's `VStack(spacing: 8) { … }` becomes `VStack(() => { … }).Spacing(8)`, so a
  setting lands after the block, sometimes far below. This is a limit of C#; short blocks and view methods keep the
  setting close (see [Code style](#code-style)).
- **Conditional views take more lines than in SwiftUI**: in a block that spans several lines, the repository style puts
  an `if` on its own lines (see [Code style](#code-style)).
- **Views held in variables are not statements**: C# only accepts calls, assignments and `new` as statements, so
  `_counter;`, `content;` or a property `Header;` do not compile (CS0201) and need `Show(x)` (see [Show](#show)).
  SwiftUI writes `CounterView(start: 3)` in the block and keeps its state by identity; doing the same would change the
  state model (state stored in the tree, not in the instance), which is a separate proposal.
- **A scoped method follows only its arguments and its state**: a plain field (not `[State]`) that changes is no longer
  refreshed with the caller. SwiftUI has the same rule; MVU010 already warns about such a field in a method that
  returns a view, and `Refresh()` reaches the view methods too (see [Scopes](#scopes)).
- **Stepping into a scoped method**: F11 on `Progress();` does not enter the method, which runs later; a breakpoint in
  it does. Running the scope at once would fix it, at the cost of skipping (see
  [Unresolved questions](#unresolved-questions)).
- **No loops in a block**: the same rule as SwiftUI, but surprising for C# developers; MVU014 and its code fix explain
  it.
- **Generator complexity**: generic methods, extension methods, private members, nested blocks, scopes.

## Risks

- **Interceptors are still opt-in** (`InterceptorsNamespaces`); the feature has been treated as experimental by the C#
  team. If its rules change, the generator must follow; if it were removed, the explicit builder of
  [Alternatives](#alternatives) is the fallback, and a code fix would rewrite the blocks.
- **Tooling not verified yet**: the spike is still to be extended to stepping in Rider and Visual Studio, Windows and
  Linux, a library in its own assembly, lambdas without braces, named arguments (`Button(label: () => { … })`), large
  files and full AOT publishing.
- **Cost not measured**: a delegate per block and per evaluation, a list per `Collect`, a scope per view method. Each
  delivery step measures, on the sample app before and after, the allocated bytes
  (`GC.GetAllocatedBytesForCurrentThread`) and the re-evaluated scopes (`ReactorTestHost.Stats`); small methods may need
  to opt out of scopes.
- **Diagnostics quality**: a compile error in a block may point to generated code if the generator is wrong.

## Alternatives

| Alternative | Example | Why not |
|---|---|---|
| Status quo | `cond ? Text("a") : null`, `Group`, helper methods | Remains available; limited for nested flow |
| Explicit builder | `VStack(v => { v += Header(); if (x) { v += Text("a"); } })` | No generator, but `v +=` before every view |
| Implicit emission (Compose) | `Text("a")` emits when called in a block, at run time | The same call would both return and emit: invisible bugs |
| Rewriting generator | Copy a template method into a generated one | The user's code would never run: dead code, broken breakpoints |
| `[ViewBuilder]` methods | `[ViewBuilder] void Details() { Text("a"); }` | Returns nothing: cannot be modified, passed or tested, and throws outside a block |
| `Func<View>` overload on containers | `VStack(() => flag ? a : b)` | Same as the argument `VStack(flag ? a : b)`: a third overload for nothing |
| Settings before the block | `VStack(spacing: 8, content: () => { … })` | Breaks "children first" and duplicates `.Spacing()` |
| `Body` as a block | `protected override void Content() { Header(); … }` | A second way to write `Body`; SwiftUI's `body` also starts with a container in practice, so only `() =>` would be saved |
| A common name such as `Add(view)` | `Add(_counter);` | Hidden by any member of the same name, such as the `Add()` action of the sample app |

## Unresolved questions

- A `switch` expression over an enum also gets CS8524 (unnamed values) unless it ends with `_ =>`, which turns off the
  check for values added later. Should MVU015 cover `switch` expressions too, so that applications can turn CS8524 off?
- What gives a row a binding to its item: `Tags.Bindings`, or `ForEach(Bind.Tags, tag => TextField(tag))` when `Tags`
  is a `[State]`, like SwiftUI's `ForEach($tags)`? A `Binding<StateList<T>>` is not an `IEnumerable<T>`, so this
  overload is not ambiguous, and it reuses the `Bind` vocabulary.
- How are the arguments of a scoped method compared: `Equals` (value equality for records and strings), with delegates
  always treated as changed?
- Should a scoped method run at once, during its caller (stepping into it works, but it is no longer skipped when the
  caller is re-evaluated, as `Observe` today), or later (skipped when its arguments are equal)?
- Which parameters get blocks, and in which order: container children (stacks, `Group`, `Menu`, `MenuBar`), then the
  labels and decorations that SwiftUI also builds with blocks (`Button(label: () => { … }, action: Save)`,
  `.Overlay(() => { … })`, templates)?
- Should builders be typed, like SwiftUI's `@CommandsBuilder` for menus (`[ViewBuilder(typeof(MenuItemView))]`), so that
  MVU014 rejects a `Text` in a `Menu` block?

## Spike results

Done on 2026-11-21 with the .NET SDK 10.0.100, outside the repository (a minimal view library, the generator and a
console application).

| Check | Result |
|---|---|
| `if` / `else`, nested blocks, generic modifier `.Bold()`, private instance and static methods | Correct views |
| `ForEach` block: overloads, keys, an `if` in a row, generic interceptor for `ForEach<T>` | Correct views |
| Overload resolution `Action` / `Func<…>` (`ForEach`, and `VStack(Func<View>)` since removed) | A block picks `Action`, an expression picks `Func<…>`, including a target-typed conditional |
| Build without `InterceptorsNamespaces` | Error CS9137 with the property to add |
| Call stack | User lines in the block and in the called method; generated frames skipped when stepping |
| Hot reload: edit a text in a block | Applied (93 ms) |
| Hot reload: add a statement at the end / in the middle | Applied (65 ms / 831 ms) |
| Hot reload: remove a statement / the last call to a method | Applied (88 ms / 30 ms) |
| Hot reload: call a newly intercepted method / add an `if` block | Applied (70 ms / 55 ms) |
| Hot reload: edit a text / add a statement in a `ForEach` block | Applied (967 ms / 139 ms) |
| Hot reload: turn an expression row into a block (the call switches overloads) | Applied (108 ms) |

Two lessons from the spike: interceptor names must be **deterministic** (a global counter forced a restart on every
edit), and must not depend on the **rank** of a call (an insertion renamed the following interceptors: `ENC0020`).

Not covered yet: [Identity](#identity), [Groups](#groups), [Scopes](#scopes) and [Fragments](#fragments) need the real
reconciler (the spike's view library has none); view methods returning a `Group`, `Show` and the cases listed under
[Risks](#risks) are still to try.
