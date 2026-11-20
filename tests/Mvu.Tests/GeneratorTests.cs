using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Mvu.Generators;
using Mvu.State;

namespace Mvu.Tests;

/// <summary>Le générateur [State] et ses diagnostics, sur des extraits de code compilés en mémoire.</summary>
public class GeneratorTests
{
    const string Usings =
        "using System; using System.Collections.Generic; using Mvu.Primitives; using Mvu.State; " +
        "using Mvu.Theming; using Mvu.Views; using static Mvu.Views.Ui;\n";

    [Fact]
    public void Valid_state_compiles_without_diagnostics()
    {
        var diags = Compile("""
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
        // Ne compile pas (pas de fonction de modification) : MVU012 dit quoi écrire, à côté de l'erreur du compilateur.
        var diagnostic = Assert.Single(Compile("""
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
        // Compile, mais la case ne ferait qu'afficher l'état : c'est le cas silencieux.
        var diagnostic = Assert.Single(Compile("""
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
                    TextField(Bind.Name, "Nom"),                         // liaison
                    TextField(Name, v => Name = v.Trim(), "Nom nettoyé"), // modification explicite
                    Checkbox(Agreed, null, "Lecture seule voulue"),       // lecture seule explicite
                    Text(Name),                                           // contrôle sans liaison possible
                    TextField(Name.ToUpper(), null));                     // valeur calculée
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
        var error = Assert.Single(Compile("""
            public class C { void M(Themes themes) => themes.Select("Sable"); }
            """), d => d.Id == "CS0619");   // membre obsolète, en erreur
        Assert.Contains(".Theme(", error.GetMessage(CultureInfo.InvariantCulture));
    }

    static string[] Ids(string code) => Compile(code).Select(d => d.Id).Distinct().ToArray();

    /// <summary>Compile l'extrait avec le générateur et l'analyseur ; renvoie avertissements et erreurs.</summary>
    static IReadOnlyList<Diagnostic> Compile(string code)
    {
        var parse = new CSharpParseOptions(LanguageVersion.Preview);
        var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var refs = tpa.Append(typeof(Component).Assembly.Location)
            .DistinctBy(Path.GetFileName)
            .Select(p => MetadataReference.CreateFromFile(p));

        var compilation = CSharpCompilation.Create("Snippet",
            [CSharpSyntaxTree.ParseText(Usings + code, parse)], refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        CSharpGeneratorDriver.Create([new StateGenerator().AsSourceGenerator()], parseOptions: parse)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        return output.WithAnalyzers([new StateAnalyzer()])
            .GetAllDiagnosticsAsync().Result
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToList();
    }
}
