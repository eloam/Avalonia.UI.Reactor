using System.Runtime.ExceptionServices;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.UI.Reactor;

using Avalonia.UI.Reactor.Hosting;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Point d'entrée d'une application : ouvre une fenêtre qui affiche un composant, rendu par les contrôles d'Avalonia.
/// <code>ReactorApp.Run&lt;CounterApp&gt;("Compteur", 400, 300);</code>
/// </summary>
public static class ReactorApp
{
    /// <summary>Ouvre <typeparamref name="TApp"/> dans une fenêtre et rend la main quand elle se ferme.</summary>
    public static void Run<TApp>(string title = "App", int width = 800, int height = 600, Themes? themes = null)
        where TApp : Component, new() =>
        Run(new TApp(), new WindowOptions { Title = title, Width = width, Height = height, Themes = themes });

    /// <summary>Même chose, avec tous les réglages de la fenêtre.</summary>
    public static void Run<TApp>(WindowOptions options) where TApp : Component, new() => Run(new TApp(), options);

    /// <summary>Ouvre un composant déjà construit (par exemple avec des paramètres).</summary>
    public static void Run(Component app, WindowOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        var window = options ?? new WindowOptions();
        RunOnUiThread(() => AppBuilder
            .Configure(() => new ReactorApplication(() => CreateWindow(app, window)))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime([]));
    }

    /// <summary>Ferme la fenêtre et termine <see cref="Run(Component, WindowOptions?)"/> (menu « Quitter », par exemple).</summary>
    public static void Quit() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();

    static Window CreateWindow(Component app, WindowOptions options) => new()
    {
        Title = options.Title,
        Width = options.Width,
        Height = options.Height,
        MinWidth = options.MinWidth,
        MinHeight = options.MinHeight,
        CanResize = options.Resizable,
        WindowDecorations = options.Borderless ? WindowDecorations.None : WindowDecorations.Full,
        WindowState = options.Fullscreen ? WindowState.FullScreen
            : options.Maximized ? WindowState.Maximized
            : WindowState.Normal,
        Content = new ReactorHost(app, options.Themes ?? Themes.Default),
    };

    /// <summary>
    /// Sous Windows, Avalonia attend un thread d'interface STA (OLE : presse-papiers, glisser-déposer). Une application
    /// écrite en instructions de niveau supérieur ne peut pas marquer son <c>Main</c> <c>[STAThread]</c> : l'interface
    /// tourne alors sur un thread STA dédié, et <see cref="Run(Component, WindowOptions?)"/> attend sa fin.
    /// Ailleurs (macOS exige le thread principal), l'interface tourne sur le thread appelant.
    /// </summary>
    static void RunOnUiThread(Action run)
    {
        if (!OperatingSystem.IsWindows() || Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            run();
            return;
        }

        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try { run(); }
            catch (Exception e) { failure = ExceptionDispatchInfo.Capture(e); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
