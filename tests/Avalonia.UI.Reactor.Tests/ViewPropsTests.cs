// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Reflection;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Shared view properties: a modifier set on a wrapped view is never lost.
/// </summary>
public class ViewPropsTests
{
    [Fact]
    public void CopyFrom_copies_every_View_property()
    {
        var properties = typeof(View).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType == typeof(View) && p.CanWrite)
            .ToList();

        View source = new BoxView();
        foreach (PropertyInfo? p in properties)
            p.SetValue(source, SampleFor(p));
        BoxView copy = new BoxView().CopyFrom(source);

        foreach (PropertyInfo? p in properties)
        {
            Assert.True(Equals(p.GetValue(copy), p.GetValue(source)),
                $"'View.{p.Name}' is not copied by 'ViewProps.CopyFrom'. Add it to that method.");
        }
    }

    [Fact]
    public void Inherit_fills_every_View_property_that_the_child_does_not_set()
    {
        // The key is the exception: the key of the group is placed above the key of the child (see the next test).
        var properties = typeof(View).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType == typeof(View) && p.CanWrite && p.Name != nameof(View.Key))
            .ToList();

        View group = new BoxView();
        foreach (PropertyInfo? p in properties)
            p.SetValue(group, SampleFor(p));
        View child = new BoxView().Inherit(group, position: 0);

        foreach (PropertyInfo? p in properties)
        {
            Assert.True(Equals(p.GetValue(child), p.GetValue(group)),
                $"'View.{p.Name}' is not filled by 'ViewProps.Inherit'. Add it to that method.");
        }
    }

    [Fact]
    public void The_key_of_a_group_is_placed_above_the_keys_or_positions_of_its_children()
    {
        StackView stack = Ui.VStack(Ui.Group(Ui.Text("a"), Ui.Text("b").Key("b")).Key("g"));

        Assert.Equal(["g/0", "g/b"], stack.Children.Select(child => child.Key));
    }

    /// <summary>
    /// Returns a non-default value for each property type of <see cref="View"/>.
    /// </summary>
    private static object SampleFor(PropertyInfo p) => (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) switch
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
        var t when t == typeof(Func<CancellationToken, Task>) => (Func<CancellationToken, Task>)(_ => Task.CompletedTask),
        var t when t == typeof(object) => 42,
        var t => throw new InvalidOperationException(
            $"New property 'View.{p.Name}' of type '{t.Name}'. Add a sample value for it to this test."),
    };
}
