// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace TodoDemo.Components;

/// <summary>
/// Represents the root of the sample: the menu bar, the header with the theme picker, and two components that each
/// have their own state.
/// </summary>
/// <remarks>
/// The chosen theme is state like any other: the view derives from it, nothing is called to apply it.
/// </remarks>
public sealed partial class TodoApp : Component
{
    // The tick of the clock of the About card.
    private static readonly TimeSpan s_clockInterval = TimeSpan.FromSeconds(1);

    private readonly CounterCard _counter = new();
    private readonly TodoList _todos = new();

    /// <inheritdoc/>
    public override View Body =>
        VStack(
                // Observed on its own: checking a task re-evaluates the bar only, not the whole screen.
                Observe(Menus),
                VStack(() =>
                    {
                        Header();
                        if (ShowAbout)
                            About();

                        // A component is not a statement: Show adds it to the block.
                        if (ShowCounter)
                            Show(_counter);
                        Show(_todos);
                    })
                    .Spacing(16)
                    .Padding(24))
            .Spacing(0)
            .Background(ColorRole.Page)
            // The whole screen in the chosen family…
            .Theme(ThemeName)
            // …in light, in dark, or following the system.
            .ColorScheme(Scheme);

    [State] private partial string ThemeName { get; set; } = DemoThemes.StandardFamily;

    // The forced color scheme, or null to follow the system.
    [State] private partial ColorScheme? Scheme { get; set; }

    [State] private partial bool ShowCounter { get; set; } = true;

    [State] private partial bool ShowAbout { get; set; }

    [State] private partial int AboutSeconds { get; set; }

    // Drawn at the top of the window, or the system menu bar when the platform has one (macOS). Check marks, disabled
    // items and lists follow the state, like the rest of the view.
    private View Menus() =>
        MenuBar(
            Menu("&File",
                MenuItem("&New idea", _todos.SuggestAsync).Shortcut("Ctrl+N"),
                MenuItem("&Purge completed tasks", _todos.Purge).Shortcut("Ctrl+Shift+P").Enabled(_todos.HasDone),
                Divider(),
                MenuItem("&Quit", MartletApp.Quit).Shortcut("Ctrl+Q")),
            Menu("&View",
                Menu("&Theme", [.. Themes.Families.Select(family =>
                    MenuItem(family.Name, () => ThemeName = family.Name).Checked(ThemeName == family.Name))]),
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
                        Text("Martlet").Font(TextStyle.Title),
                        // The row takes the whole width: the pickers go to the right.
                        Spacer(),
                        // The families are read from the catalog: adding one adds it here.
                        Segmented(Bind.ThemeName, [.. Themes.Families.Select(family => (family.Name, family.Name))]),
                        Segmented(Bind.Scheme,
                            ("System", null), ("Light", ColorScheme.Light), ("Dark", ColorScheme.Dark)))
                    .Spacing(12),
                Text("Declarative C# UI, no XAML, rendered by Avalonia").Foreground(ColorRole.TextMuted))
            .Spacing(2);

    private View About() =>
        Card(
            Text("About").Font(TextStyle.Headline),
            Text("Views and state in C#, rendered by Avalonia's controls.").Foreground(ColorRole.TextMuted),
            // Observed on its own, re-evaluated alone every second. The clock starts when the card opens, and its task is
            // cancelled when the card closes: nothing is left running.
            Observe(() => Text($"Open for {AboutSeconds} s").Foreground(ColorRole.TextMuted))
                .OnAppear(() => AboutSeconds = 0)
                .Task(async cancellation =>
                {
                    using var timer = new PeriodicTimer(s_clockInterval);
                    while (await timer.WaitForNextTickAsync(cancellation))
                        AboutSeconds++;
                }),
            HStack(Spacer(), Button("Close", () => ShowAbout = false).Style(ButtonStyle.Secondary)));
}
