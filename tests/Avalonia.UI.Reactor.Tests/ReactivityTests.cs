using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>Recalcul sélectif : seul ce qui lit un état modifié est réévalué.</summary>
public class ReactivityTests
{
    [Fact]
    public void Demo_recomputes_only_what_depends_on_the_changed_state()
    {
        using var t = new TestHost(new TodoApp(), themes: DemoThemes.Create());

        t.ClickButton("+");
        Assert.Equal(1, t.Stats.Evaluated); // la carte compteur seule

        t.Click(v => v is TextFieldView);
        t.Type("x");
        Assert.Equal(1, t.Stats.Evaluated); // la ligne de saisie seule

        t.ClickText("Ajouter un backend mobile");
        Assert.Equal(3, t.Stats.Evaluated); // la ligne cochée + le pied « N restante(s) » + l'anneau de progression
    }

    [Fact]
    public void Unchanged_scopes_reuse_their_layout()
    {
        using var t = new TestHost(new TodoApp(), themes: DemoThemes.Create());
        t.ClickButton("+");
        Assert.True(t.Stats.Reused >= 1); // la liste de tâches n'est ni réévaluée ni remesurée
    }

    [Fact]
    public void State_changed_from_a_background_thread_updates_the_view()
    {
        var probe = new Probe();
        using var t = new TestHost(probe);

        var worker = new Thread(() => probe.N = 42);
        worker.Start();
        worker.Join();
        t.WaitUntil(() => t.HasText("N = 42"));
    }

    [Fact]
    public void Setting_the_same_value_does_not_recompute()
    {
        var probe = new Probe();
        using var t = new TestHost(probe);
        probe.N = 0;
        t.Ui.Invalidate();
        Assert.Equal(1, t.Stats.Evaluated); // toujours le bilan du premier rendu : aucune mise à jour
    }

    [Fact]
    public void StateList_changes_update_the_view()
    {
        var list = new ListProbe();
        using var t = new TestHost(list);

        list.Items.Add("b");
        t.Ui.Invalidate();
        Assert.True(t.HasText("b"));
        Assert.Equal(3, t.Stats.Scopes); // la racine + une portée par ligne

        list.Items.Remove("a");
        t.Ui.Invalidate();
        Assert.False(t.HasText("a"));
        Assert.Equal(2, t.Stats.Scopes); // la portée de la ligne disparue est libérée
    }

    [Fact]
    public void Refresh_updates_a_component_with_plain_fields()
    {
        var legacy = new LegacyProbe();
        using var t = new TestHost(legacy);

        legacy.Set(7);
        t.Ui.Invalidate();
        Assert.True(t.HasText("7"));
    }

    [Fact]
    public void Nested_component_keeps_its_state_when_parent_recomputes()
    {
        var parent = new ParentProbe();
        using var t = new TestHost(parent);

        t.ClickButton("enfant 0");
        t.ClickButton("parent 0");
        Assert.True(t.HasButton("parent 1"));
        Assert.True(t.HasButton("enfant 1"));
    }
}
