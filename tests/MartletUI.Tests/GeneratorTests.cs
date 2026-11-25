// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.CodeAnalysis;
using static MartletUI.Tests.Snippets;

namespace MartletUI.Tests;

/// <summary>
/// The [State] generator and its diagnostics, on code snippets compiled in memory.
/// </summary>
public class GeneratorTests
{
    [Fact]
    public void Valid_state_compiles_without_diagnostics()
    {
        IReadOnlyList<Diagnostic> diags = Compile("""
            public partial class Counter : Component
            {
                [State] partial int Count { get; set; }
                [State] public partial string Name { get; private set; } = "";
                public override View Body => Button($"{Name} {Count}", () => Count++);
            }
            public partial class Outer { public partial class Model { [State] public partial bool Done { get; set; } } }
            """);
        Assert.Empty(diags);
    }

    [Fact]
    public void State_property_must_be_partial() =>
        Assert.Contains("MVU001", Ids("""
            public partial class C : Component
            {
                [State] int Count { get; set; }
                public override View Body => Text($"{Count}");
            }
            """));

    [Fact]
    public void Containing_type_must_be_partial() =>
        Assert.Contains("MVU002", Ids("""
            public class C : Component
            {
                [State] partial int Count { get; set; }
                public override View Body => Text("");
            }
            """));

    [Fact]
    public void State_property_must_not_have_a_body() =>
        Assert.Contains("MVU003", Ids("""
            public partial class C : Component
            {
                [State] partial int Count { get => 1; set { } }
                public override View Body => Text("");
            }
            """));

    [Fact]
    public void Displayed_plain_field_modified_in_an_action_warns() =>
        Assert.Contains("MVU010", Ids("""
            public class C : Component
            {
                int count;
                public override View Body => Button($"{count}", () => count++);
            }
            """));

    [Fact]
    public void Displayed_plain_list_modified_warns() =>
        Assert.Contains("MVU011", Ids("""
            public class C : Component
            {
                readonly List<string> items = new();
                public override View Body => VStack(Text($"{items.Count}"), Button("+", () => items.Add("x")));
            }
            """));

    [Fact]
    public void Displayed_collections_changed_in_place_warn_with_what_to_write()
    {
        string[] messages = [.. Compile("""
            public partial class C : Component
            {
                [State] partial List<string> Tags { get; set; } = [];
                [State] partial string[] Colors { get; set; } = [];
                private readonly Dictionary<string, int> _scores = [];
                private HashSet<int> Picked { get; } = [];

                public override View Body => VStack(() =>
                {
                    Text($"{Tags.Count} {Colors.Length} {_scores.Count} {Picked.Count}");
                    Button("Edit", () =>
                    {
                        Tags.Add("x");
                        Colors[0] = "red";
                        _scores["a"] += 1;
                        Picked.Add(1);
                    });
                });
            }
            """).Where(d => d.Id == "MVU011").Select(d => d.GetMessage(CultureInfo.InvariantCulture)).Order()];

        Assert.Equal(
            [
                "Collection '_scores' is read by the view but is not observable, so changing it in place does not update the UI; assign a new collection to a [State] property instead, or call 'Refresh()'",
                "Collection 'Colors' is read by the view but is not observable, so changing it in place does not update the UI; use 'StateList<string>'",
                "Collection 'Picked' is read by the view but is not observable, so changing it in place does not update the UI; assign a new collection to a [State] property instead, or call 'Refresh()'",
                "Collection 'Tags' is read by the view but is not observable, so changing it in place does not update the UI; use 'StateList<string>'",
            ],
            messages);
    }

    [Fact]
    public void Observed_or_replaced_collections_do_not_warn() =>
        Assert.Empty(Ids("""
            public partial class C : Component
            {
                [State] partial System.Collections.Immutable.ImmutableArray<string> Tags { get; set; } = [];
                [State] partial string[] Colors { get; set; } = [];
                private readonly StateList<int> _numbers = [];
                private readonly List<string> _log = [];

                public override View Body => VStack(() =>
                {
                    Text($"{Tags.Length} {Colors.Length} {_numbers.Count} {_log.Count}");
                    Button("Edit", () =>
                    {
                        Tags = Tags.Add("x");
                        Colors = [.. Colors, "red"];
                        _numbers.Add(1);
                        _log.Add("edited");
                        Refresh();
                    });
                });
            }
            """));

    [Fact]
    public void Field_not_displayed_does_not_warn() =>
        Assert.Empty(Ids("""
            public class C : Component
            {
                int clicks;
                public override View Body => Button("+", () => clicks++);
            }
            """));

    [Fact]
    public void Plain_field_with_Refresh_does_not_warn() =>
        Assert.Empty(Ids("""
            public class C : Component
            {
                int value;
                public void Set(int v) { value = v; Refresh(); }
                public override View Body => Text($"{value}");
            }
            """));

    [Fact]
    public void Bindings_are_generated_for_private_and_public_states() =>
        Assert.Empty(Compile("""
            public partial class Form : Component
            {
                [State] partial string Draft { get; set; } = "";
                [State] partial bool Agreed { get; set; }
                public override View Body => VStack(TextField(Bind.Draft, "Nom"), Checkbox(Bind.Agreed));
            }
            public partial class Todo { [State] public partial bool Done { get; set; } }
            public class List : Component
            {
                readonly Todo todo = new();
                public override View Body => Checkbox(todo.Bind.Done);
            }
            """));

    [Fact]
    public void Bindings_of_private_states_stay_private() =>
        Assert.Contains("CS0122", Ids("""
            public partial class Form : Component
            {
                [State] partial string Draft { get; set; } = "";
                public override View Body => Text(Draft);
            }
            public class Other { object M(Form f) => f.Bind; }
            """));

    [Fact]
    public void Derived_component_bindings_hide_the_base_ones_without_warning() =>
        Assert.Empty(Compile("""
            public partial class Base : Component
            {
                [State] public partial int A { get; set; }
                public override View Body => Text($"{A}");
            }
            public partial class Derived : Base
            {
                [State] public partial int B { get; set; }
            }
            """));

    [Fact]
    public void Init_only_states_get_no_binding() =>
        Assert.Contains("CS0103", Ids("""
            public partial class C : Component
            {
                [State] public partial string Name { get; init; } = "";
                public override View Body => TextField(Bind.Name, "x");
            }
            """));

    [Fact]
    public void State_passed_to_a_TextField_without_its_binding_says_what_to_write()
    {
        // Does not compile (no change function): MVU012 says what to write, next to the compiler error.
        Diagnostic diagnostic = Assert.Single(Compile("""
            public partial class Form : Component
            {
                [State] partial string Name { get; set; } = "";
                public override View Body => TextField(Name, "Nom");
            }
            """), d => d.Id == "MVU012");
        Assert.Contains("Bind.Name", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void State_passed_to_a_Checkbox_without_its_binding_is_reported_even_though_it_compiles()
    {
        // Compiles, but the checkbox would only display the state: the silent case.
        Diagnostic diagnostic = Assert.Single(Compile("""
            public partial class Todo { [State] public partial bool Done { get; set; } }
            public class List : Component
            {
                readonly Todo todo = new();
                public override View Body => Checkbox(todo.Done, label: "Fait");
            }
            """));
        Assert.Equal("MVU012", diagnostic.Id);
        Assert.Contains("todo.Bind.Done", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Bound_or_explicit_uses_do_not_warn() =>
        Assert.Empty(Ids("""
            public partial class Form : Component
            {
                [State] partial string Name { get; set; } = "";
                [State] partial bool Agreed { get; set; }
                public override View Body => VStack(
                    TextField(Bind.Name, "Nom"),                         // Binding.
                    TextField(Name, v => Name = v.Trim(), "Nom nettoyé"), // Explicit change function.
                    Checkbox(Agreed, null, "Lecture seule voulue"),       // Explicitly read-only.
                    Text(Name),                                           // Control that cannot take a binding.
                    TextField(Name.ToUpper(), null));                     // Computed value.
            }
            """));

    [Fact]
    public void View_created_as_a_statement_is_reported_with_what_to_write()
    {
        Diagnostic diagnostic = Assert.Single(Compile("""
            public class C : Component
            {
                public override View Body => VStack(Header());

                private View Header()
                {
                    Text("Title");
                    return Divider();
                }
            }
            """));
        Assert.Equal("MVU013", diagnostic.Id);
        string message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains("'Text'", message);
        Assert.Contains("'VStack'", message);
    }

    [Fact]
    public void View_lost_in_an_action_is_reported() =>
        Assert.Contains("MVU013", Ids("""
            public class C : Component
            {
                public override View Body => Button("Add", () => Text("Added"));
            }
            """));

    [Fact]
    public void Used_or_discarded_views_are_not_reported() =>
        Assert.Empty(Ids("""
            public class C : Component
            {
                public override View Body => VStack(Header());

                private View Header()
                {
                    var title = Text("Title");
                    _ = Text("Ignored on purpose");
                    return title;
                }
            }
            """));

    [Fact]
    public void View_statements_of_a_block_are_not_reported() =>
        Assert.Empty(Ids("""
            public partial class C : Component
            {
                [State] partial string Email { get; set; } = "";
                public override View Body => VStack(() =>
                {
                    Text("Title").Bold();
                    if (Email.Length == 0)
                        Text("Required");
                    TextField(Bind.Email, "Email");
                    HStack(content: () => Text("Named"));
                });
            }
            """));

    [Fact]
    public void View_methods_of_every_kind_compile_in_a_block() =>
        Assert.Empty(Compile("""
            public static class Views
            {
                public static View Labeled<T>(T value, params string[] labels) => Text($"{labels.Length} {value}");

                public static View Framed(this View content) => VStack(content).Padding(4);

                public static StackView Column(View content) => VStack(content);

                public static View Wide(int a, int b, int c, int d, int e, int f, int g, int h, int i) => Text($"{a + i}");
            }

            public partial class Page : Component
            {
                [State] partial int Count { get; set; }

                public override View Body => VStack(() =>
                {
                    Counter();
                    Counter().Padding(8);
                    Views.Labeled(Count, "a", "b");
                    Text("Framed").Framed();
                    Views.Column(Text("Column")).Spacing(4);
                    Views.Wide(1, 2, 3, 4, 5, 6, 7, 8, 9);
                    Pair<int>(Count);
                });

                private View Counter() => Text($"{Count}");

                internal static View Pair<T>(T value) => Text($"{value}");
            }
            """));

    [Fact]
    public void View_statements_outside_the_block_itself_are_reported()
    {
        IReadOnlyList<Diagnostic> diagnostics = Compile("""
            public class C : Component
            {
                public override View Body => VStack(() =>
                {
                    Button("Add", () => { Text("In an action"); });
                    foreach (string name in new[] { "a" })
                        Text("In a loop");
                });
            }
            """);

        // The view of the action is lost; the loop is reported as a whole by MVU014.
        Assert.Equal(["MVU013", "MVU014"], diagnostics.Select(d => d.Id).Order());
    }

    [Fact]
    public void Component_with_state_created_in_a_view_is_reported_with_what_to_write()
    {
        Diagnostic diagnostic = Assert.Single(Compile("""
            public partial class Counter : Component
            {
                [State] partial int Count { get; set; }
                public override View Body => Text($"{Count}");
            }

            public class Page : Component
            {
                public override View Body => VStack(new Counter());
            }
            """));
        Assert.Equal("MVU016", diagnostic.Id);
        string message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains("'Counter'", message);
        Assert.Contains("field", message);
    }

    [Fact]
    public void Component_with_state_created_for_each_row_is_reported() =>
        Assert.Contains("MVU016", Ids("""
            public partial class Row : Component
            {
                [State] partial bool Open { get; set; }
                public override View Body => Text(Open ? "open" : "closed");
            }

            public class Page : Component
            {
                private readonly int[] _items = [1, 2];
                public override View Body => VStack(ForEach(_items, item => new Row()));
            }
            """));

    [Fact]
    public void Components_kept_stateless_or_created_in_actions_are_not_reported() =>
        Assert.Empty(Ids("""
            public partial class Counter : Component
            {
                [State] partial int Count { get; set; }
                public override View Body => Text($"{Count}");
            }

            public class Label : Component
            {
                public override View Body => Text("Label");
            }

            public class Page : Component
            {
                private readonly Counter _counter = new();
                private readonly List<Counter> _added = [];
                private Counter? _lazy;

                public override View Body => VStack(
                    _counter,
                    _lazy ??= new Counter(),
                    new Label(),
                    Button("Add", () => _added.Add(new Counter())));
            }
            """));

    [Fact]
    public void Task_that_ignores_its_token_warns_with_what_to_write()
    {
        Diagnostic diagnostic = Assert.Single(Compile("""
            public class C : Component
            {
                public override View Body => Text("x").Task(async _ => await System.Threading.Tasks.Task.Delay(1000));
            }
            """));
        Assert.Equal("MVU017", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("pass the token", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Task_of_a_component_that_ignores_its_token_warns() =>
        Assert.Contains("MVU017", Ids("""
            public class Child : Component { public override View Body => Text("child"); }
            public class C : Component
            {
                private readonly Child _child = new();
                public override View Body => _child.Task("key", async _ => await System.Threading.Tasks.Task.Delay(1000));
            }
            """));

    [Fact]
    public void Task_that_uses_its_token_or_is_a_method_does_not_warn() =>
        Assert.Empty(Ids("""
            public class C : Component
            {
                public override View Body => VStack(
                    Text("a").Task(async cancellation => await System.Threading.Tasks.Task.Delay(1000, cancellation)),
                    Text("b").Task("key", async cancellation => await System.Threading.Tasks.Task.Delay(1000, cancellation)),
                    Text("c").Task(RunAsync));

                private static System.Threading.Tasks.Task RunAsync(System.Threading.CancellationToken cancellation) =>
                    System.Threading.Tasks.Task.CompletedTask;
            }
            """));

    [Fact]
    public void Bind_and_StateBindings_are_reserved_in_classes_with_state()
    {
        var reserved = Compile("""
            public partial class Form : Component
            {
                [State] partial string Draft { get; set; } = "";
                private StateBindings Bind => new();
                private readonly struct StateBindings { }
                public override View Body => Text(Draft);
            }
            """).Where(d => d.Id == "MVU004").ToList();

        Assert.Equal(2, reserved.Count);
        Assert.Contains(reserved, d => d.GetMessage(CultureInfo.InvariantCulture).StartsWith("'Bind' is reserved", StringComparison.Ordinal));
        Assert.Contains(reserved, d => d.GetMessage(CultureInfo.InvariantCulture).StartsWith("'StateBindings' is reserved", StringComparison.Ordinal));
    }

    [Fact]
    public void Reserved_names_are_free_in_classes_without_settable_state() =>
        Assert.DoesNotContain("MVU004", Ids("""
            public class Plain { public int Bind => 1; }
            public partial class InitOnly
            {
                [State] public partial string Name { get; init; } = "";
                public string StateBindings => "";
            }
            """));

    [Fact]
    public void Themes_Select_points_to_the_declarative_way()
    {
        Diagnostic error = Assert.Single(Compile("""
            public class C { void M(Themes themes) => themes.Select("Sable"); }
            // Obsolete member, as an error.
            """), d => d.Id == "CS0619");
        Assert.Contains(".Theme(", error.GetMessage(CultureInfo.InvariantCulture));
    }
}
