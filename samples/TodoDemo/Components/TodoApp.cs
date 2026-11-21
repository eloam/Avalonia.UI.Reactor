// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace TodoDemo.Components;

/// <summary>
/// The root: the menu bar, the header with the theme picker, and two components that each have their own state.
/// The chosen theme is state like any other: the view derives from it, nothing is called to apply it.
/// </summary>
public sealed partial class TodoApp : Component
{
    [State] private partial string ThemeName { get; set; } = "Standard";

    /// <summary>
    /// Gets or sets the forced color scheme; <see langword="null"/> for the system one.
    /// </summary>
    [State]
    private partial ColorScheme? Scheme { get; set; }

    [State] private partial bool ShowCounter { get; set; } = true;
    [State] private partial bool ShowAbout { get; set; }

    private readonly CounterCard _counter = new();
    private readonly TodoList _todos = new();

    public override View Body =>
        VStack(
                // A separate block: checking a task re-evaluates the bar only, not the whole screen.
                Observe(Menus),
                VStack(Header(), ShowAbout ? About() : null, ShowCounter ? _counter : null, _todos)
                    .Spacing(16)
                    .Padding(24))
            .Spacing(0)
            .Background(ColorRole.Page)
            // The whole screen in the chosen family…
            .Theme(ThemeName)
            // …in light, in dark, or following the system.
            .ColorScheme(Scheme);

    /// <summary>
    /// Drawn at the top of the window, or the system menu bar when the platform has one (macOS).
    /// Check marks, disabled items and lists follow the state, like the rest of the view.
    /// </summary>
    private View Menus() =>
        MenuBar(
            Menu("&File",
                MenuItem("&New idea", _todos.SuggestAsync).Shortcut("Ctrl+N"),
                MenuItem("&Purge completed tasks", _todos.Purge).Shortcut("Ctrl+Shift+P").Enabled(_todos.HasDone),
                Divider(),
                MenuItem("&Quit", ReactorApp.Quit).Shortcut("Ctrl+Q")),
            Menu("&View",
                Menu("&Theme", [.. Themes.Families.Select(f =>
                    MenuItem(f.Name, () => ThemeName = f.Name).Checked(ThemeName == f.Name))]),
                Menu("&Appearance",
                    MenuItem("&System", () => Scheme = null).Checked(Scheme is null),
                    MenuItem("&Light", () => Scheme = ColorScheme.Light).Checked(Scheme == ColorScheme.Light),
                    MenuItem("&Dark", () => Scheme = ColorScheme.Dark).Checked(Scheme == ColorScheme.Dark)),
                Divider(),
                MenuItem("&Counter", Bind.ShowCounter).Shortcut("Ctrl+K")),
            Menu("&Help",
                MenuItem("&About", () => ShowAbout = true).Shortcut("F1")));

    private View Header() =>
        VStack(
                HStack(
                        Text("Reactor").Font(TextStyle.Title),
                        // The row takes the whole width: the picker goes to the right.
                        Spacer(),
                        // The families are read from the catalog: adding one adds it here.
                        Segmented(Bind.ThemeName, [.. Themes.Families.Select(f => (f.Name, f.Name))]),
                        Segmented(Bind.Scheme,
                            ("System", null), ("Light", ColorScheme.Light), ("Dark", ColorScheme.Dark)))
                    .Spacing(12),
                Text("Declarative C# UI, no XAML, rendered by Avalonia").Foreground(ColorRole.TextMuted))
            .Spacing(2);

    private View About() =>
        Card(
            Text("About").Font(TextStyle.Headline),
            Text("Views and state in C#, rendered by Avalonia's controls.")
                .Foreground(ColorRole.TextMuted),
            HStack(Spacer(), Button("Close", () => ShowAbout = false).Style(ButtonStyle.Secondary)));
}
