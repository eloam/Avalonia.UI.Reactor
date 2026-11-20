using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;

/// <summary>Recalcul sélectif : seules les portées qui ont lu un état modifié sont réévaluées, quel que soit le thread.</summary>
public class ReactivityTests
{
    [AvaloniaFact]
    public void A_state_changed_from_a_background_thread_updates_the_view()
    {
        var probe = new CounterProbe();
        using var t = new ReactorTestHost(probe);

        var worker = new Thread(() => probe.Count = 42);
        worker.Start();
        worker.Join();

        t.WaitUntil(() => t.HasText("42"));
    }

    [AvaloniaFact]
    public void Setting_the_same_value_reevaluates_nothing()
    {
        var probe = new CounterProbe();
        using var t = new ReactorTestHost(probe);

        probe.Count = 0;
        t.Settle();

        Assert.Equal(1, t.Stats.Evaluated); // toujours le bilan du premier rendu : aucune mise à jour
    }

    [AvaloniaFact]
    public void Adding_or_removing_an_item_updates_the_list_and_its_scopes()
    {
        var probe = new ListProbe();
        using var t = new ReactorTestHost(probe);
        Assert.Equal(4, t.Stats.Scopes); // la racine et une portée par ligne

        probe.Items.Add("d");
        t.Settle();
        Assert.True(t.HasText("d"));
        Assert.Equal(5, t.Stats.Scopes);

        probe.Items.Remove("a");
        t.Settle();
        Assert.False(t.HasText("a"));
        Assert.Equal(4, t.Stats.Scopes); // la portée de la ligne retirée est libérée
    }

    [AvaloniaFact]
    public void Refresh_updates_a_component_without_state_properties()
    {
        var probe = new RefreshProbe();
        using var t = new ReactorTestHost(probe);

        probe.Set(7);
        t.Settle();

        Assert.True(t.HasText("7"));
    }

    [AvaloniaFact]
    public void A_child_component_keeps_its_state_when_its_parent_is_reevaluated()
    {
        using var t = new ReactorTestHost(new ParentProbe());

        t.ClickButton("enfant 0");
        t.ClickButton("parent 0");

        Assert.True(t.HasButton("parent 1"));
        Assert.True(t.HasButton("enfant 1"));
    }
}
