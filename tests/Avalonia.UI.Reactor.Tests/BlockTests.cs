// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// View builder blocks: <c>if</c>, <c>switch</c> and local variables in a view, as with <c>@ViewBuilder</c> in SwiftUI,
/// lowered by the generator of the package.
/// </summary>
public class BlockTests
{
    [AvaloniaFact]
    public void The_view_statements_of_a_block_become_its_children_in_order()
    {
        using var t = new ReactorTestHost(new BlockProbe());

        string[] expected = ["En-tête", "Invité", "Accueil", "Ligne", "Dans la carte", "Détail 1", "2 éléments", "Liste non vide", "a", "b", "b!", "Pied"];
        Assert.Equal(expected, t.Texts.Select(text => text.Text).Where(expected.Contains));
        Assert.True(t.HasButton("Connexion"));
        Assert.False(t.HasText("Bienvenue"));
        Assert.False(t.HasText("Détail 2"));
    }

    [AvaloniaFact]
    public void A_branch_follows_the_state()
    {
        using var t = new ReactorTestHost(new BlockProbe());

        t.ClickButton("Connexion");

        FontWeight bold = t.Texts.Single(text => text.Text == "Bienvenue").FontWeight;
        Assert.True(bold > t.Texts.Single(text => text.Text == "Accueil").FontWeight);
        Assert.True(t.HasText("Détail 2"));
        Assert.False(t.HasText("Invité"));
        Assert.False(t.HasButton("Connexion"));
    }

    [AvaloniaFact]
    public void A_switch_shows_the_views_of_its_case()
    {
        var probe = new BlockProbe();
        using var t = new ReactorTestHost(probe);

        probe.Page = 2;
        t.Settle();

        Assert.True(t.HasText("Page 2"));
        Assert.False(t.HasText("Accueil"));
    }

    [AvaloniaFact]
    public void A_field_keeps_its_control_and_focus_when_a_branch_opens_before_it()
    {
        var probe = new BlockProbe();
        using var t = new ReactorTestHost(probe);
        TextBox field = t.Field;
        t.Click(field);
        t.Type("abc");

        probe.HasError = true;
        t.Settle();

        Assert.True(t.HasText("Erreur"));
        Assert.Same(field, t.Field);
        Assert.True(field.IsFocused);
        Assert.Equal("abc", probe.Email);
    }

    [AvaloniaFact]
    public void A_row_of_a_list_block_follows_its_items()
    {
        var probe = new BlockProbe();
        using var t = new ReactorTestHost(probe);

        probe.Items.Add("c");
        probe.Items.Remove("b");
        t.Settle();

        Assert.True(t.HasText("c"));
        Assert.False(t.HasText("b"));
        Assert.False(t.HasText("b!"));
    }

    [AvaloniaFact]
    public void An_early_return_ends_the_block()
    {
        var probe = new BlockProbe();
        using var t = new ReactorTestHost(probe);

        probe.Items.Clear();
        t.Settle();

        Assert.True(t.HasText("0 éléments"));
        Assert.False(t.HasText("Liste non vide"));
    }

    [Fact]
    public void A_block_compiled_without_the_generator_is_reported()
    {
        // A method of the runtime library: its assembly was not compiled with the generator.
        Action block = GC.Collect;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Ui.VStack(block));

        Assert.Contains("compiled without the generator", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_view_emitted_outside_a_container_is_reported()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => ViewCollector.Emit(Ui.Text("x")));

        Assert.Contains("'VStack'", error.Message, StringComparison.Ordinal);
    }
}
