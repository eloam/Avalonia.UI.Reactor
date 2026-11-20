using System.Runtime.CompilerServices;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Tests.Probes;
using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Une fenêtre fermée ne reste attachée ni aux états ni aux composants qu'elle affichait, et ne reste pas en mémoire
/// quand son catalogue de thèmes ou ses composants lui survivent.
/// </summary>
public class LifetimeTests
{
    [AvaloniaFact]
    public void Closing_the_window_detaches_the_components_and_keeps_their_state()
    {
        var probe = new CounterProbe();
        var t = new ReactorTestHost(probe);
        t.ClickButton("+");
        Assert.NotNull(probe.Host);

        t.Dispose();
        probe.Count++; // un état modifié après la fermeture ne réveille plus la fenêtre

        Assert.Null(probe.Host);
        Assert.Equal(2, probe.Count);
    }

    [AvaloniaFact]
    public void A_closed_window_is_released_although_its_theme_catalog_and_component_live_on()
    {
        var themes = new Themes(Theme.Light, Theme.Dark);
        var component = new TaskListProbe(); // ses états (signaux) survivent à la fenêtre

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
    static WeakReference OpenAndClose(TaskListProbe component, Themes themes)
    {
        using var t = new ReactorTestHost(component, themes: themes);
        // Un état change sans passer par le pointeur : un clic laisse une minuterie de geste d'Avalonia en attente dans
        // le répartiteur sans écran, qui garde l'élément appuyé (et sa fenêtre) jusqu'à ce qu'elle se déclenche.
        component.Tasks[1].Done = !component.Tasks[1].Done;
        t.Settle();
        return new WeakReference(t.Host);
    }

    /// <summary>
    /// Sans écran, aucun tic de rendu n'envoie le dernier lot du compositeur d'Avalonia : il garde la dernière fenêtre
    /// fermée jusqu'au rendu suivant. Un tic forcé l'envoie, comme le ferait l'écran d'une application.
    /// </summary>
    static void SendPendingCompositionBatch()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
