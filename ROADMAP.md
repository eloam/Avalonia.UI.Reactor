# Roadmap

This document describes where Martlet is heading. It is a statement of intent, not a commitment: priorities
change with feedback, and items may move between milestones or be dropped. Large features are first written as a
[specification](spec/README.md), discussed, then built.

*Last updated: November 2026.*

## Current status

Martlet is **experimental**. The core programming model is in place (components, `[State]`, view builder
blocks, fine-grained updates, themes, analyzers), but no package is published yet and the framework has only been run on
macOS. The milestones below lead to a first preview, then to a stable 1.0.

## Milestones

### 0.1 — First preview

Make the framework usable outside this repository.

- [ ] Continuous integration on macOS, Windows and Linux, running the headless tests on every pull request
- [ ] Run and verify the sample app on Windows and Linux
- [ ] NuGet packages: `MartletUI`, `MartletUI.Core` (with the generators, analyzers and code fixes)
- [ ] Measure the memory of the engine and explain the idle CPU usage (1 to 10 ms per second on macOS)
- [ ] Public API for the errors of actions, instead of the standard error output only

### 0.2 — Lists and navigation

The controls that every non-trivial application needs.

- [ ] `ScrollView` and virtualized lists ([specification](spec/README.md) first)
- [ ] Navigation: `NavigationStack` and modal sheets ([specification](spec/README.md) first)
- [ ] Context menu declared in the view
- [ ] Cancellation token for asynchronous actions ([proposal](spec/lifecycle.md#cancelling-actions))

### 0.3 — Input and media

- [ ] Input controls: `Toggle`, `Picker`, `Slider`, multiline text
- [ ] Images
- [ ] File dialogs
- [ ] Embedded fonts
- [ ] Save the theme chosen by the user

### 0.4 — Programming model

- [ ] An environment for the data of the application, like SwiftUI's `@Environment` or React's context
  ([specification](spec/README.md) first)
- [ ] An error boundary: a view of your own in place of one that fails (its errors already stay in its scope)
- [ ] Animations and transitions ([specification](spec/README.md) first)
- [ ] Accessibility: labels and roles, through the automation peers of Avalonia

### 1.0 — Stable

- [ ] API review against the .NET Framework Design Guidelines, then no breaking change without a major version
- [ ] Complete documentation and samples
- [ ] Performance budgets checked in continuous integration

## Later and under consideration

Ideas that need a specification or a spike before they are scheduled.

- Android, iOS and browser (WebAssembly) hosts
- Components created in the view (`CounterCard();` without a field, their state kept in the tree, as in SwiftUI)
- The open questions of the view builder blocks: typed builders for menus, blocks for labels and decorations,
  `Binding<T>` compared by its target ([list](spec/view-builder.md#unresolved-questions))

## Shipped

Recent work, newest first. See the [commit history](https://github.com/eloam/MartletUI/commits/main) for
details.

- [x] An exception in a view stays in its scope: the rest of the window is updated
- [x] State changed while a view is computed is an error (MVU018, and an exception at run time), not an endless update
- [x] Bindings to the items of a list: `ForEach(Tags.Bind, tag => TextField(tag))`
- [x] MVU011 covers every collection changed in place, `[State]` properties included
- [x] View methods called in a block get their own scope, re-evaluated alone, like SwiftUI subviews; `Observe` blocks
- [x] `Show(…)`, the MVU014 and MVU015 diagnostics, and three code fixes in the IDE
- [x] View builder blocks: `VStack(() => { … })` with `if`, `switch` and local variables
- [x] Fragments: the views of a component or a row take their places in the parent container
- [x] Group modifiers applied to each child
- [x] View lifecycle: `.OnAppear`, `.OnDisappear` and `.Task`, cancelled when the view disappears
- [x] Child identity by key, component, binding, then type and rank: controls keep their focus when a sibling appears
- [x] Rendering with Avalonia's controls, replacing the original engine

## Influencing the roadmap

- **Feedback and requests**: open an [issue](https://github.com/eloam/MartletUI/issues) describing what you
  are trying to build and what is missing.
- **Large features**: start from a [specification](spec/README.md), in the format of the C# language proposals.
- **Contributions**: see [CONTRIBUTING.md](CONTRIBUTING.md). Items of the next milestone are the most welcome.
