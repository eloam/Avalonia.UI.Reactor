# Contributing to Avalonia.UI.Reactor

Thank you for your interest in contributing! This document explains how to build the project, the conventions the
code follows, and what a pull request needs before it can be merged.

## Reporting issues

Before opening an issue, search the existing ones. A good bug report includes the operating system, the .NET SDK
version (`dotnet --version`), a minimal component that reproduces the problem, and what you expected to happen.

## Building and testing

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build Avalonia.UI.Reactor.sln
dotnet test Avalonia.UI.Reactor.sln
dotnet run --project samples/TodoDemo
```

If the sample app is running (for example in a debugger) and locks the output files, build elsewhere with
`--artifacts-path <temporary folder>`.

Tests open real Avalonia windows without a screen (Avalonia.Headless) through `ReactorTestHost`: they click, hover and
type through the platform input, and read texts, bounds and pixels. Put each topic in its own `*Tests.cs` file and the
small test components in `Probes/`.

## Design principles

1. **Simple and intuitive**: the common case fits on one line, without `new` and without ceremony.
2. **Everything customizable**: no visual value is hard-coded; colors, sizes and control looks come from the theme.
3. **One UI dependency, hidden**: `Avalonia.UI.Reactor.Core` exposes no Avalonia type.

When in doubt about an API, look at SwiftUI and Jetpack Compose, then simplify further.

## Coding style

The repository follows Microsoft's [C# coding conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
and the [dotnet/runtime coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md),
for code and comments alike. [.editorconfig](.editorconfig) describes the rules and the build enforces them; run
`dotnet format Avalonia.UI.Reactor.sln` to apply most of them automatically.

- Every file starts with the license header; namespaces are file-scoped; braces follow the Allman style, and an
  embedded statement without braces goes on its own line.
- Accessibility modifiers are always written. Instance fields are `_camelCase`, static fields `s_camelCase`, constants
  `PascalCase`. No `this.`; language keywords (`string`, `int`) instead of type names; `var` only when the type is
  apparent from the right-hand side.
- Members are ordered: fields, constructors, events, properties, methods; public before private.
- Asynchronous methods end with `Async` (except `async void` handlers). A method named `Dispose` implies `IDisposable`.
- Everything is in English: identifiers (named like SwiftUI: `Button`, `VStack`, `Theme`), comments and documentation.
- Comments are complete sentences, with a capital letter and a final period, on their own line above the code.
- Every public member of the libraries has XML documentation in the style of the .NET API documentation: a
  `<summary>` in the third person ("Gets or sets…", "Initializes a new instance of the X class."), then `<param>`,
  `<returns>` and `<exception>`; details go in `<remarks>` and samples in `<example>`. A missing comment is a build
  error (CS1591).

## API conventions

- One type per file; the folder gives the namespace.
- Views and themes are immutable records, customized with `with` and chainable `WithXxx` methods.
- View factories live in `Ui` (no `new` in views) and take their children first; settings go through modifiers.
- Every modifier exists in `Modifiers` (generic) **and** `ComponentModifiers` (same signature).
- A control with a value gets a `Binding<T>` overload.
- `internal` by default; a public type follows the .NET Framework Design Guidelines and is designed to last.
- Diagnostics, exceptions and logs are in English, in the style of Microsoft's messages. Logs start with `[Reactor]`.

## Quality bar

- The build treats warnings as errors, runs the .NET analyzers and enforces the code style: fix a warning, do not
  suppress it. In `samples/`, warnings stay warnings while you edit, and are fixed before a commit as well.
- No magic numbers or strings: use a named constant or a theme value.
- No dead code, legacy overloads or duplicated APIs.
- Every fixed bug comes with a test that fails without the fix.
- A workaround for a third-party limitation lives in a named method, with a comment that says what, why and when to
  remove it.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/), in English: `type(scope): subject`, in the
imperative mood, without a final period, at most 72 characters. Explain what and why in the body. Mark breaking
changes with `!` and a `BREAKING CHANGE:` footer.

```
feat(avalonia): add menu bar and text field context menu
fix(reconciler): keep the caret when a field is re-rendered
refactor!: remove the legacy SDL3 and Skia engine
```

## Pull request checklist

- [ ] `dotnet test` passes and the build has no warnings.
- [ ] Tests cover the new behavior (and the pixels, for a visual change).
- [ ] Checked by eye in the Standard (light, dark) and Sable (light, night) themes for visual changes.
- [ ] The sample app and the documentation (`README.md`, `docs/`) are up to date.

## License

By contributing, you agree that your contributions are licensed under the [MIT license](LICENSE).
