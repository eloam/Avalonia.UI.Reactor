using System.Reflection;
using Mvu.Primitives;
using Mvu.Theming;
using Mvu.Views;

namespace Mvu.Tests;

/// <summary>Écriture des piles : enfants d'abord, souplesse automatique, groupes et ForEach mélangés.</summary>
public class StackSyntaxTests
{
    // ── Une pile s'étire toute seule si un enfant s'étire ──

    [Fact]
    public void Spacer_stretches_its_row_without_FillWidth()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            HStack(Text("a"), Spacer(), Text("b").Key("b")))), width: 300);

        Assert.Equal(300, t.BoundsOfKey("b").Right, precision: 2);
    }

    [Fact]
    public void Row_without_flexible_child_keeps_its_natural_width()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            HStack(Text("a"), Text("b").Key("b")))), width: 300);

        Assert.True(t.BoundsOfKey("b").Right < 100);
    }

    [Fact]
    public void Growing_child_stretches_its_row()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            HStack(TextField("", null).Grow(), Button("OK").Key("ok")))), width: 300);

        Assert.Equal(300, t.BoundsOfKey("ok").Right, precision: 2);
    }

    [Fact]
    public void Vertical_spacer_stretches_its_column()
    {
        using var t = new TestHost(new ViewProbe(() => HStack(
            VStack(Text("haut"), Spacer(), Text("bas").Key("bas")))), width: 300, height: 200);

        Assert.Equal(200, t.BoundsOfKey("bas").Bottom, precision: 2);
    }

    [Fact]
    public void Explicit_size_wins_over_flexible_children()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            HStack(Text("a"), Spacer(), Text("b").Key("b")).Width(100))), width: 300);

        Assert.Equal(100, t.BoundsOfKey("b").Right, precision: 2);
    }

    // ── Espacement et alignement en modificateurs ──

    [Fact]
    public void Spacing_and_alignment_are_modifiers()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            VStack(Box().Size(10, 10).Key("a"), Box().Size(10, 10).Key("b")).Spacing(5),
            ZStack(Box().Size(40, 40), Box().Size(10, 10).Key("coin")).Alignment(Alignment.BottomRight))
            .Spacing(0)));

        Assert.Equal(15, t.BoundsOfKey("b").Y);
        Assert.Equal(new Rect(30, 55, 10, 10), t.BoundsOfKey("coin"));
    }

    // ── Groupes et ForEach ──

    static readonly string[] Letters = ["x", "y"];
    static readonly string[] ExpectedOrder = ["début", "x", "y", "fin"];

    [Fact]
    public void ForEach_mixes_with_other_children()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            Text("début").Key("début"),
            ForEach(Letters, s => Text(s).Key(s)),
            Text("fin").Key("fin")).Spacing(10)));

        float[] ys = [.. ExpectedOrder.Select(k => t.BoundsOfKey(k).Y)];
        Assert.Equal(ys.Order(), ys);                                   // dans l'ordre…
        float line = t.BoundsOfKey("début").Height;
        Assert.Equal(line + 10, t.BoundsOfKey("x").Y, precision: 2);    // …avec l'espacement de la pile entre chacun
    }

    [Fact]
    public void Nested_groups_are_unfolded_in_place()
    {
        using var t = new TestHost(new ViewProbe(() => HStack(
            Text("a").Key("a"),
            Group(Text("b").Key("b"), Group(Text("c").Key("c"))))));

        // Dépliés dans la ligne : côte à côte, à la même hauteur.
        Assert.Equal(t.BoundsOfKey("a").Y, t.BoundsOfKey("c").Y);
        Assert.True(t.BoundsOfKey("b").X > t.BoundsOfKey("a").X);
        Assert.True(t.BoundsOfKey("c").X > t.BoundsOfKey("b").X);
    }

    [Fact]
    public void Group_with_modifiers_or_alone_stacks_vertically()
    {
        using var t = new TestHost(new ViewProbe(() => HStack(
            Group(Text("a").Key("a"), Text("b").Key("b")).Padding(4))));

        Assert.True(t.BoundsOfKey("b").Y > t.BoundsOfKey("a").Y);
        Assert.Equal(4, t.BoundsOfKey("a").X, precision: 2);
    }

    // ── Recopie des propriétés communes ──

    [Fact]
    public void CopyFrom_copies_every_View_property()
    {
        var properties = typeof(View).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType == typeof(View) && p.CanWrite)
            .ToList();

        View source = new BoxView();
        foreach (var p in properties) p.SetValue(source, SampleFor(p));
        var copy = new BoxView().CopyFrom(source);

        foreach (var p in properties)
            Assert.True(Equals(p.GetValue(copy), p.GetValue(source)),
                $"'View.{p.Name}' is not copied by 'ViewProps.CopyFrom'. Add it to that method.");
    }

    /// <summary>Une valeur non par défaut pour chaque type de propriété de <see cref="View"/>.</summary>
    static object SampleFor(PropertyInfo p) => (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) switch
    {
        var t when t == typeof(string) => "exemple",
        var t when t == typeof(float) => 3f,
        var t when t == typeof(bool) => true,
        var t when t == typeof(Thickness) => new Thickness(1, 2, 3, 4),
        var t when t == typeof(Align) => Align.End,
        var t when t == typeof(ColorValue) => (ColorValue)"#123456",
        var t when t == typeof(TextStyle) => TextStyle.Title,
        var t when t == typeof(Theme) => Theme.Dark,
        var t when t == typeof(ThemeFamily) => ThemeFamily.Standard,
        var t when t == typeof(ColorScheme) => ColorScheme.Dark,
        var t when t == typeof(Action) => (Action)(() => { }),
        var t => throw new InvalidOperationException(
            $"New property 'View.{p.Name}' of type '{t.Name}'. Add a sample value for it to this test."),
    };
}
