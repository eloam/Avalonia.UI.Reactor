// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace TodoDemo.Components;

/// <summary>
/// Represents the task list: an input row, the tasks that the filter shows, and a footer.
/// </summary>
/// <remarks>
/// The parts that change often are observed on their own: typing re-evaluates the input row only, and checking a task
/// re-evaluates its row, the footer and the progress ring.
/// </remarks>
public sealed partial class TodoList : Component
{
    // The time a suggestion takes, as a network call would.
    private static readonly TimeSpan s_thinkingTime = TimeSpan.FromMilliseconds(800);

    private static readonly string[] s_ideas = ["Write the tests", "Add a ScrollView", "Prepare the demo", "Measure performance"];

    private readonly StateList<Todo> _todos =
    [
        new("Write the layout engine", done: true),
        new("Hook up the Skia GPU renderer", done: true),
        new("Add a mobile backend"),
    ];

    /// <summary>
    /// Gets a value indicating whether at least one task is done; the "Purge" menu item is disabled otherwise.
    /// </summary>
    public bool HasDone => _todos.Any(todo => todo.Done);

    /// <inheritdoc/>
    public override View Body => Card(() =>
    {
        HStack(
            Text("Tasks").Font(TextStyle.Headline),
            Spacer(),
            // Observed on its own: checking a task redraws the ring, not the card.
            Observe(() => ProgressRing(_todos.Count(todo => todo.Done), _todos.Count)));

        // Observed on its own: typing re-evaluates this row only.
        Observe(() => HStack(
            TextField(Bind.Draft, "What needs to be done?", onSubmit: Add).Grow(),
            // A button style added by the theme.
            Button("Add", Add).Style(DemoThemes.Success),
            Button(Thinking ? "…" : "Idea", SuggestAsync).Style(ButtonStyle.Secondary)));

        // A local variable and an if decide which views are shown, as in a SwiftUI view builder.
        List<Todo> visible = [.. _todos.Where(IsVisible)];
        if (visible.Count == 0)
            Text("Nothing to show").Foreground(ColorRole.TextMuted).Padding(4, 8);
        else
            VStack(ForEach(visible, Row)).Spacing(0);

        Divider();
        Observe(Footer);
    });

    [State] private partial string Draft { get; set; } = "";

    [State] private partial bool Thinking { get; set; }

    [State] private partial Filter Filter { get; set; }

    /// <summary>
    /// Removes the completed tasks.
    /// </summary>
    public void Purge() => _todos.RemoveAll(todo => todo.Done);

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
        try
        {
            await Task.Delay(s_thinkingTime);
            _todos.Add(new(s_ideas[Random.Shared.Next(s_ideas.Length)]));
        }
        finally
        {
            Thinking = false;
        }
    }

    private bool IsVisible(Todo todo) => Filter switch
    {
        Filter.Active => !todo.Done,
        Filter.Done => todo.Done,
        _ => true,
    };

    private void Add()
    {
        if (string.IsNullOrWhiteSpace(Draft))
            return;

        _todos.Add(new(Draft.Trim()));
        Draft = "";
    }

    private View Row(Todo todo) =>
        HStack(
                Checkbox(todo.Bind.Done),
                Text(todo.Title).Foreground(todo.Done ? ColorRole.TextMuted : ColorRole.Text).Grow(),
                Button("×", () => _todos.Remove(todo)).Style(ButtonStyle.Ghost).Padding(8, 0).FontSize(18))
            .Padding(6, 4)
            .Radius(6)
            .OnTap(() => todo.Done = !todo.Done);

    private View Footer() => HStack(() =>
    {
        // A text style added by the theme.
        Text($"{_todos.Count(todo => !todo.Done)} left").Foreground(ColorRole.TextMuted).Font(DemoThemes.Caption);
        Spacer();
        Segmented(Bind.Filter, ("All", Filter.All), ("Active", Filter.Active), ("Done", Filter.Done));
        if (HasDone)
            Button("Purge", Purge).Style(ButtonStyle.Danger).Padding(10, 4).Font(TextStyle.Small);
    });
}
