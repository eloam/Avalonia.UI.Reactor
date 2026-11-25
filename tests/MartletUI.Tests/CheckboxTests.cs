// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless.XUnit;
using MartletUI.Tests.Probes;

namespace MartletUI.Tests;

/// <summary>
/// A checkbox is a real Avalonia CheckBox bound to state; it handles its own click.
/// </summary>
public class CheckboxTests
{
    [AvaloniaFact]
    public void Clicking_a_checkbox_inside_a_tappable_row_changes_the_state_once()
    {
        var probe = new TaskRowProbe();
        using var t = new MartletTestHost(probe);

        t.Click(t.CheckBoxLabeled(null));

        // The clickable row did not toggle it a second time.
        Assert.True(probe.Done);
        Assert.True(t.CheckBoxLabeled(null).IsChecked);
    }

    [AvaloniaFact]
    public void Tapping_the_row_beside_the_checkbox_runs_the_row_action()
    {
        var probe = new TaskRowProbe();
        using var t = new MartletTestHost(probe);

        t.Click(t.TextShowing("tâche"));

        Assert.True(probe.Done);
        Assert.True(t.CheckBoxLabeled(null).IsChecked);
    }

    [AvaloniaFact]
    public void A_checkbox_without_action_keeps_the_value_of_its_view()
    {
        using var t = new MartletTestHost(new TaskRowProbe());

        t.Click(t.CheckBoxLabeled("figée"));

        Assert.False(t.CheckBoxLabeled("figée").IsChecked);
    }

    [AvaloniaFact]
    public void A_checkbox_bound_to_a_model_object_toggles_only_its_own_state()
    {
        var probe = new TaskListProbe();
        using var t = new MartletTestHost(probe);

        t.Click(t.CheckBoxLabeled("Écrire les tests"));

        Assert.True(probe.Tasks[0].Done);
        Assert.False(probe.Tasks[1].Done);
    }
}
