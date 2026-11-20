using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;

/// <summary>Une case est une vraie CheckBox d'Avalonia, liée à l'état ; elle consomme son clic.</summary>
public class CheckboxTests
{
    [AvaloniaFact]
    public void Clicking_a_checkbox_inside_a_tappable_row_changes_the_state_once()
    {
        var probe = new TaskRowProbe();
        using var t = new ReactorTestHost(probe);

        t.Click(t.CheckBoxLabeled(null));

        Assert.True(probe.Done); // la ligne cliquable n'a pas inversé une seconde fois
        Assert.True(t.CheckBoxLabeled(null).IsChecked);
    }

    [AvaloniaFact]
    public void Tapping_the_row_beside_the_checkbox_runs_the_row_action()
    {
        var probe = new TaskRowProbe();
        using var t = new ReactorTestHost(probe);

        t.Click(t.TextShowing("tâche"));

        Assert.True(probe.Done);
        Assert.True(t.CheckBoxLabeled(null).IsChecked);
    }

    [AvaloniaFact]
    public void A_checkbox_without_action_keeps_the_value_of_its_view()
    {
        using var t = new ReactorTestHost(new TaskRowProbe());

        t.Click(t.CheckBoxLabeled("figée"));

        Assert.False(t.CheckBoxLabeled("figée").IsChecked);
    }

    [AvaloniaFact]
    public void A_checkbox_bound_to_a_model_object_toggles_only_its_own_state()
    {
        var probe = new TaskListProbe();
        using var t = new ReactorTestHost(probe);

        t.Click(t.CheckBoxLabeled("Écrire les tests"));

        Assert.True(probe.Tasks[0].Done);
        Assert.False(probe.Tasks[1].Done);
    }
}
