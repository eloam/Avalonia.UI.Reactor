// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Runtime.ExceptionServices;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.UI.Reactor;

using Avalonia.UI.Reactor.Hosting;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Provides the entry point of an application: a window that displays a component, rendered by Avalonia's controls.
/// </summary>
/// <example>
/// <code>
/// ReactorApp.Run&lt;CounterApp&gt;("Counter", 400, 300);
/// </code>
/// </example>
public static class ReactorApp
{
    /// <summary>
    /// Shows a new instance of a component in a window, and returns when the window closes.
    /// </summary>
    /// <typeparam name="TApp">The type of the root component.</typeparam>
    /// <param name="title">The title of the window.</param>
    /// <param name="width">The initial width, in device-independent units.</param>
    /// <param name="height">The initial height, in device-independent units.</param>
    /// <param name="themes">The theme catalog, or <see langword="null"/> for the built-in Light and Dark themes.</param>
    public static void Run<TApp>(string title = "App", int width = 800, int height = 600, Themes? themes = null)
        where TApp : Component, new() =>
        Run(new TApp(), new WindowOptions { Title = title, Width = width, Height = height, Themes = themes });

    /// <summary>
    /// Shows a new instance of a component in a window with all the window settings, and returns when the window closes.
    /// </summary>
    /// <typeparam name="TApp">The type of the root component.</typeparam>
    /// <param name="options">The settings of the window.</param>
    public static void Run<TApp>(WindowOptions options)
        where TApp : Component, new() => Run(new TApp(), options);

    /// <summary>
    /// Shows a component built by the caller (for example with arguments) in a window, and returns when the window closes.
    /// </summary>
    /// <param name="app">The root component.</param>
    /// <param name="options">The settings of the window, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static void Run(Component app, WindowOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        WindowOptions window = options ?? new WindowOptions();
        RunOnUiThread(() => AppBuilder
            .Configure(() => new ReactorApplication(() => CreateWindow(app, window)))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime([]));
    }

    /// <summary>
    /// Closes the window and ends <see cref="Run(Component, WindowOptions?)"/>, for example from a Quit menu item.
    /// </summary>
    public static void Quit() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();

    private static Window CreateWindow(Component app, WindowOptions options) => new()
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
        Content = new ReactorHost(app, options.Themes ?? Themes.Default)
        {
            Zoom = options.Zoom,
            ZoomShortcuts = options.ZoomShortcuts,
        },
    };

    // On Windows, Avalonia expects a single-threaded apartment for the user interface thread (OLE: clipboard, drag and
    // drop). An application written with top-level statements cannot mark its Main method [STAThread], so the interface
    // then runs on a dedicated STA thread, and Run waits for it to end. Elsewhere (macOS requires the main thread), the
    // interface runs on the calling thread.
    private static void RunOnUiThread(Action run)
    {
        if (!OperatingSystem.IsWindows() || Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            run();
            return;
        }

        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                run();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
