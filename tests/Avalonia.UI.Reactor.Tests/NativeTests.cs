// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;

/// <summary>
/// <c>Native(…)</c>: any Avalonia control, created once and updated from the state.
/// </summary>
public class NativeTests
{
    [AvaloniaFact]
    public void A_native_control_is_created_once_and_follows_the_states_its_update_reads()
    {
        var probe = new NativeProbe();
        using var t = new ReactorTestHost(probe);
        Slider slider = t.Controls.OfType<Slider>().Single();
        Assert.Equal(10, slider.Value);
        Assert.Equal(200, ((Control)slider.Parent!).Bounds.Width);

        probe.Level = 60;
        t.Settle();

        Assert.Same(slider, t.Controls.OfType<Slider>().Single());
        Assert.Equal(60, slider.Value);
        // Only the update of the slider ran again, not the view.
        Assert.Equal(1, probe.Evaluations);
    }

    [AvaloniaFact]
    public void A_native_control_follows_its_state_after_gaining_or_losing_a_decoration()
    {
        // Gaining or losing a background moves the control into or out of a surface, which detaches it from the visual
        // tree and attaches it again: its update must still follow the state afterwards.
        var probe = new DecoratedNativeProbe();
        using var t = new ReactorTestHost(probe);
        Slider slider = t.Controls.OfType<Slider>().Single();

        probe.Highlighted = true;
        t.Settle();
        probe.Level = 60;
        t.Settle();

        Assert.Same(slider, t.Controls.OfType<Slider>().Single());
        Assert.Equal(60, slider.Value);

        probe.Highlighted = false;
        t.Settle();
        probe.Level = 30;
        t.Settle();

        Assert.Same(slider, t.Controls.OfType<Slider>().Single());
        Assert.Equal(30, slider.Value);
    }

    [AvaloniaFact]
    public void An_update_requested_before_the_window_closes_does_not_run_after_it()
    {
        var probe = new DecoratedNativeProbe();
        var t = new ReactorTestHost(probe);
        int updates = probe.Updates;

        // The change requests a new run of the update; the window closes before the dispatcher runs it.
        probe.Level = 60;
        t.Dispose();

        Assert.Equal(updates, probe.Updates);
    }

    [AvaloniaFact]
    public void Another_control_type_at_the_same_place_replaces_the_control()
    {
        var probe = new NativeProbe();
        using var t = new ReactorTestHost(probe);
        Assert.Single(t.Controls.OfType<Calendar>());

        probe.UseProgress = true;
        t.Settle();

        Assert.Empty(t.Controls.OfType<Calendar>());
        Assert.Single(t.Controls.OfType<ProgressBar>());
    }
}
