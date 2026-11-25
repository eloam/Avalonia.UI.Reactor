// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.CodeAnalysis;
using static MartletUI.Tests.Snippets;

namespace MartletUI.Tests;

/// <summary>
/// The diagnostics of view builder blocks (MVU014, MVU015), of <c>Show</c>, and the rules of the other diagnostics inside
/// a block, on code snippets compiled in memory.
/// </summary>
public class ViewBuilderAnalyzerTests
{
    [Fact]
    public void Statements_of_a_block_that_add_no_view_are_reported_with_what_to_write()
    {
        string[] messages = Messages("MVU014", """
            public static class Views<T>
            {
                public static View Make() => Text(typeof(T).Name);
            }

            public partial class C : Component
            {
                [State] partial int Count { get; set; }

                public override View Body => VStack(() =>
                {
                    Count++;
                    foreach (string name in new[] { "a" })
                        Text(name);
                    Views<int>.Make();
                    try
                    {
                        Text("b");
                    }
                    finally
                    {
                    }
                });
            }
            """);

        Assert.Equal(
            [
                "'Count++' does not add a view to the block; move it out of the block, for example into an action",
                "'foreach' does not add a view to the block; show a view per item with 'ForEach(items, item => { … })'",
                "'Make' does not add a view to the block; wrap the view in 'Show(…)'",
                "'try' does not add a view to the block; move it out of the block, for example into an action",
            ],
            messages);
    }

    [Fact]
    public void Statements_that_describe_views_are_accepted()
    {
        Assert.Empty(Compile("""
            public enum Tab { Home, Settings }

            public partial class C : Component
            {
                [State] partial Tab Tab { get; set; }
                [State] partial string Name { get; set; } = "";
                private readonly Counter _counter = new();

                public override View Body => VStack(() =>
                {
                    string greeting = $"Hello, {Name}!";
                    System.Diagnostics.Debug.WriteLine(greeting);
                    _ = greeting.Length;
                    if (Name.Length == 0)
                    {
                        Text("Who are you?");
                        return;
                    }

                    Text(greeting).Bold();
                    Show(_counter);
                    Show(Tab == Tab.Home ? Text("Home") : null);
                    switch (Tab)
                    {
                        case Tab.Home:
                            Text("Home");
                            break;
                        case Tab.Settings:
                            throw new InvalidOperationException("Not yet.");
                    }
                });
            }

            public partial class Counter : Component
            {
                [State] partial int Count { get; set; }
                public override View Body => Text($"{Count}");
            }
            """));
    }

    [Fact]
    public void Async_block_is_reported()
    {
        string[] messages = Messages("MVU014", """
            public class C : Component
            {
                public override View Body => VStack(async () => Text("a"));
            }
            """);

        Assert.Equal(
            ["A view builder block cannot be 'async', because the views it adds after an 'await' are lost; remove 'async' and start the work with '.Task(…)'"],
            messages);
    }

    [Fact]
    public void Switch_of_a_block_that_misses_values_is_reported_with_the_values()
    {
        string[] messages = Messages("MVU015", """
            public enum Tab { Home, Settings, Profile, Help }

            public partial class C : Component
            {
                [State] partial Tab Tab { get; set; }

                public override View Body => VStack(() =>
                {
                    switch (Tab)
                    {
                        case Tab.Home:
                            Text("Home");
                            break;
                        case Tab.Help when Tab == Tab.Help:
                            Text("Help");
                            break;
                    }
                });
            }
            """);

        Assert.Equal(
            ["The switch on 'Tab' does not handle 'Tab.Settings', 'Tab.Profile' and 'Tab.Help', so these values show nothing; add a 'case' for each of them, or a 'default' section"],
            messages);
    }

    [Fact]
    public void Switch_that_handles_every_value_is_not_reported() =>
        Assert.Empty(Compile("""
            public enum Tab { Home, Settings, Profile }

            [Flags]
            public enum Style { None = 0, Bold = 1, Italic = 2 }

            public partial class C : Component
            {
                [State] partial Tab Tab { get; set; }
                [State] partial Style Style { get; set; }

                public override View Body => VStack(() =>
                {
                    switch (Tab)
                    {
                        case Tab.Home:
                            Text("Home");
                            break;
                        case Tab.Settings or Tab.Profile:
                            Text("Account");
                            break;
                    }

                    switch (Tab)
                    {
                        case Tab.Home:
                            Text("Home");
                            break;
                        default:
                            Text("Other");
                            break;
                    }

                    switch (Style)
                    {
                        case Style.Bold:
                            Text("Bold");
                            break;
                    }
                });
            }
            """));

    [Fact]
    public void Show_outside_a_block_is_reported()
    {
        string[] messages = Messages("MVU013", """
            public class C : Component
            {
                public override View Body => Button("Add", () => Show(Text("Added")));
            }
            """);

        Assert.Equal(
            ["'Show' is not called in a view builder block, so its view is never displayed; call it in a block such as 'VStack(() => { … })', or pass the view to a container"],
            messages);
    }

    [Fact]
    public void Component_with_state_created_in_a_block_is_reported() =>
        Assert.Contains("MVU016", Ids("""
            public partial class Counter : Component
            {
                [State] partial int Count { get; set; }
                public override View Body => Text($"{Count}");
            }

            public class Page : Component
            {
                public override View Body => VStack(() => Show(new Counter()));
            }
            """));

    [Fact]
    public void Plain_field_displayed_by_a_block_is_reported() =>
        Assert.Contains("MVU010", Ids("""
            public class C : Component
            {
                private int _count;

                public override View Body => VStack(() =>
                {
                    Text($"{_count}");
                    Button("+", () => _count++);
                });
            }
            """));

    private static string[] Messages(string id, string code) =>
        [.. Compile(code).Where(d => d.Id == id).Select(d => d.GetMessage(CultureInfo.InvariantCulture))];
}
