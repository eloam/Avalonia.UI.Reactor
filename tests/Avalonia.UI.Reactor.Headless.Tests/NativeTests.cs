using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Headless.Tests.Probes;

/// <summary><c>Native(…)</c> : un contrôle Avalonia quelconque, créé une fois, accordé à l'état par sa mise à jour.</summary>
public class NativeTests
{
    [AvaloniaFact]
    public void A_native_control_is_created_once_and_follows_the_states_its_update_reads()
    {
        var probe = new NativeProbe();
        using var t = new ReactorTestHost(probe);
        var slider = t.Controls.OfType<Slider>().Single();
        Assert.Equal(10, slider.Value);
        Assert.Equal(200, ((Control)slider.Parent!).Bounds.Width);

        probe.Level = 60;
        t.Settle();

        Assert.Same(slider, t.Controls.OfType<Slider>().Single());
        Assert.Equal(60, slider.Value);
        Assert.Equal(1, probe.Evaluations); // seule la mise à jour du curseur a été refaite, pas la vue
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
