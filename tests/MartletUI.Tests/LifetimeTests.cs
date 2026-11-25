// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MartletUI.Tests.Probes;
using MartletUI.Theming;

namespace MartletUI.Tests;

/// <summary>
/// A closed window stays attached neither to the state nor to the components it displayed, and does not stay in memory
/// when its theme catalog or its components outlive it.
/// </summary>
public class LifetimeTests
{
    [AvaloniaFact]
    public void Closing_the_window_detaches_the_components_and_keeps_their_state()
    {
        var probe = new CounterProbe();
        var t = new MartletTestHost(probe);
        t.ClickButton("+");
        Assert.NotNull(probe.Host);

        t.Dispose();
        // State changed after closing no longer wakes the window up.
        probe.Count++;

        Assert.Null(probe.Host);
        Assert.Equal(2, probe.Count);
    }

    [AvaloniaFact]
    public void A_closed_window_is_released_although_its_theme_catalog_and_component_live_on()
    {
        var themes = new Themes(Theme.Light, Theme.Dark);
        // Its state (signals) outlives the window.
        var component = new TaskListProbe();

        var hosts = Enumerable.Range(0, 5).Select(_ => OpenAndClose(component, themes)).ToList();
        SendPendingCompositionBatch();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.All(hosts, h => Assert.False(h.IsAlive, "A closed window should not be kept alive."));
        GC.KeepAlive(themes);
        GC.KeepAlive(component);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference OpenAndClose(TaskListProbe component, Themes themes)
    {
        using var t = new MartletTestHost(component, themes: themes);
        // State changes without the pointer: in the headless dispatcher, a click leaves an Avalonia gesture timer pending,
        // which keeps the pressed element (and its window) alive until it fires.
        component.Tasks[1].Done = !component.Tasks[1].Done;
        t.Settle();
        return new WeakReference(t.Host);
    }

    /// <summary>
    /// Without a screen, no render tick sends the last batch of the Avalonia compositor: it keeps the last closed window
    /// until the next rendering. A forced tick sends it, as the display of an application would.
    /// </summary>
    private static void SendPendingCompositionBatch()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
