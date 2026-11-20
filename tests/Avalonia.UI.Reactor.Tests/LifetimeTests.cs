using System.Runtime.CompilerServices;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>Durée de vie : une fenêtre fermée ne reste pas en mémoire, même si son catalogue ou ses états lui survivent.</summary>
public class LifetimeTests
{
    [Fact]
    public void Closed_window_is_released_although_its_theme_catalog_and_component_live_on()
    {
        var sharedThemes = new Themes(Theme.Light, Theme.Dark);
        var sharedComponent = new TaskListProbe(); // ses états (signaux) survivent à la fenêtre

        var windows = Enumerable.Range(0, 5).Select(_ => OpenAndClose(sharedComponent, sharedThemes)).ToList();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.All(windows, w => Assert.False(w.IsAlive, "A closed window should not be kept alive."));
        GC.KeepAlive(sharedThemes);
        GC.KeepAlive(sharedComponent);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference OpenAndClose(TaskListProbe component, Themes themes)
    {
        using var t = new TestHost(component, themes: themes);
        t.Paint();
        return new WeakReference(t.Ui);
    }
}
