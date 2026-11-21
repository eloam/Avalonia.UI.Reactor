// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace TodoDemo.Components;

public sealed partial class TodoList : Component
{
    [State] private partial string Draft { get; set; } = "";
    [State] private partial bool Thinking { get; set; }
    [State] private partial Filter Filter { get; set; }

    private readonly StateList<Todo> _todos =
    [
        new("Write the layout engine", done: true),
        new("Hook up the Skia GPU renderer", done: true),
        new("Add a mobile backend"),
    ];

    public override View Body
    {
        get
        {
            var visible = _todos.Where(t => Filter switch
            {
                Filter.Active => !t.Done,
                Filter.Done => t.Done,
                _ => true,
            }).ToList();

            return Card(
                HStack(
                    Text("Tasks").Font(TextStyle.Headline),
                    Spacer(),
                    Observe(() => ProgressRing(_todos.Count(t => t.Done), _todos.Count))),
                // A separately observed block: typing text re-evaluates this row only.
                Observe(() => HStack(
                    TextField(Bind.Draft, "What needs to be done?", onSubmit: Add).Grow(),
                    // A button style added by the theme.
                    Button("Add", Add).Style("success"),
                    Button(Thinking ? "…" : "Idea", SuggestAsync).Style(ButtonStyle.Secondary))),
                visible.Count == 0
                    ? Text("Nothing to show").Foreground(ColorRole.TextMuted).Padding(4, 8)
                    : VStack(ForEach(visible, Row, t => t.Id)).Spacing(0),
                Divider(),
                Observe(Footer));
        }
    }

    private View Row(Todo t) =>
        HStack(
            Checkbox(t.Bind.Done),
            Text(t.Title).Foreground(t.Done ? ColorRole.TextMuted : ColorRole.Text).Grow(),
            Button("×", () => _todos.Remove(t)).Style(ButtonStyle.Ghost).Padding(8, 0).FontSize(18))
        .Padding(6, 4)
        .Radius(6)
        .OnTap(() => t.Done = !t.Done);

    private View Footer() =>
        HStack(
            // A text style added by the theme.
            Text($"{_todos.Count(t => !t.Done)} left").Foreground(ColorRole.TextMuted).Font("caption"),
            Spacer(),
            Segmented(Bind.Filter, ("All", Filter.All), ("Active", Filter.Active), ("Done", Filter.Done)),
            _todos.Any(t => t.Done)
                ? Button("Purge", Purge)
                    .Style(ButtonStyle.Danger).Padding(10, 4).Font(TextStyle.Small)
                : null);

    /// <summary>
    /// Gets a value indicating whether at least one task is done (the "Purge" menu item is disabled otherwise).
    /// </summary>
    public bool HasDone => _todos.Any(t => t.Done);

    public void Purge() => _todos.RemoveAll(t => t.Done);

    private void Add()
    {
        if (string.IsNullOrWhiteSpace(Draft))
            return;
        _todos.Add(new(Draft.Trim()));
        Draft = "";
    }

    /// <summary>
    /// Adds a suggested task after a simulated network call.
    /// </summary>
    /// <remarks>
    /// An asynchronous action: the interface updates before and after the await, with nothing more to do. An exception
    /// here would be reported by the host, without closing the application.
    /// </remarks>
    /// <returns>A task that completes when the suggestion is added.</returns>
    public async Task SuggestAsync()
    {
        Thinking = true;
        // Simulates a network call.
        await Task.Delay(800);
        _todos.Add(new(s_ideas[Random.Shared.Next(s_ideas.Length)]));
        Thinking = false;
    }

    private static readonly string[] s_ideas = ["Write the tests", "Add a ScrollView", "Prepare the demo", "Measure performance"];
}
