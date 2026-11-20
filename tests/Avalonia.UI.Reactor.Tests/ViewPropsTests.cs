using System.Reflection;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>Propriétés communes des vues : un modificateur posé sur une vue enveloppée n'est jamais perdu.</summary>
public class ViewPropsTests
{
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
