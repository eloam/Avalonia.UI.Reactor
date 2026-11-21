// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.UI.Reactor.Hosting;

using Avalonia.UI.Reactor.Menus;
using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Reconciliation;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents the Avalonia control that displays a component: it owns the reconciler and applies the updates requested
/// by state changes.
/// </summary>
/// <remarks>
/// <para>
/// The action of a control (a click, a keystroke) is followed immediately by its update, so that the next event sees the
/// new state; a change that comes from elsewhere (the continuation of an <see langword="await"/>, another thread)
/// schedules the update on the user interface thread.
/// </para>
/// <para>
/// An error in an action, or later on the user interface thread (the continuation of an <see langword="async"/>
/// <see langword="void"/> method), is reported without closing the application. The host also applies the zoom of the
/// interface (Ctrl + / Ctrl − / Ctrl 0, Cmd on macOS).
/// </para>
/// </remarks>
internal sealed class ReactorHost : Border, IScopeHost
{
    /// <summary>
    /// The smallest zoom of the interface (50 %).
    /// </summary>
    internal const double MinZoom = 0.5;

    /// <summary>
    /// The largest zoom of the interface (300 %).
    /// </summary>
    internal const double MaxZoom = 3;

    /// <summary>
    /// The step of the zoom shortcuts (10 %).
    /// </summary>
    internal const double ZoomStep = 0.1;

    private static readonly Primitives.KeyGesture[] s_zoomInGestures =
    [
        new(KeyCode.Plus, Primitives.KeyModifiers.Ctrl),
        new(KeyCode.Equal, Primitives.KeyModifiers.Ctrl),
    ];

    private static readonly Primitives.KeyGesture s_zoomOutGesture = new(KeyCode.Minus, Primitives.KeyModifiers.Ctrl);
    private static readonly Primitives.KeyGesture s_zoomResetGesture = new(KeyCode.D0, Primitives.KeyModifiers.Ctrl);

    private readonly View _root;
    private readonly Reconciler _reconciler;
    private readonly Action _themesChanged;
    private readonly EventHandler<PlatformColorValues> _systemColorsChanged;
    private readonly EventHandler<KeyEventArgs> _keyDown;
    private readonly DispatcherUnhandledExceptionEventHandler _dispatcherError;
    private readonly LayoutTransformControl _zoom = new();
    private readonly ThemeVariantScope _variant = new();
    private IPlatformSettings? _platform;
    private TopLevel? _window;
    private int _updatePending = 1;
    private bool _attached;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorHost"/> class.
    /// </summary>
    /// <param name="root">The root component.</param>
    /// <param name="themes">The theme catalog of the application.</param>
    public ReactorHost(Component root, Themes themes)
    {
        _root = root;
        Themes = themes;
        _reconciler = new Reconciler(this, Run);
        _themesChanged = RequestUpdate;
        _systemColorsChanged = (_, colors) => Themes.SystemScheme = SchemeOf(colors);
        _keyDown = OnKeyDown;
        _dispatcherError = OnDispatcherError;
        _zoom.Child = _variant;
        Child = _zoom;
    }

    /// <summary>
    /// Occurs when an action throws, synchronously or asynchronously. Without a subscriber, the error is written to the
    /// standard error output and to the debugger.
    /// </summary>
    public event Action<Exception>? UnhandledException;

    /// <summary>
    /// Gets the theme catalog of the application.
    /// </summary>
    public Themes Themes { get; }

    /// <summary>
    /// Gets or sets the zoom of the interface, from <see cref="MinZoom"/> to <see cref="MaxZoom"/> (1 is 100 %), on top
    /// of the scale factor of the display: the layout is computed again at the new size and text stays sharp.
    /// </summary>
    public double Zoom
    {
        get;
        set
        {
            double zoom = Math.Clamp(value, MinZoom, MaxZoom);
            if (zoom == field)
            {
                return;
            }

            field = zoom;
            _zoom.LayoutTransform = zoom == 1 ? null : new ScaleTransform(zoom, zoom);
        }
    } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether Ctrl + / Ctrl − / Ctrl 0 (Cmd on macOS) zoom in, zoom out and reset the
    /// zoom.
    /// </summary>
    public bool ZoomShortcuts { get; set; } = true;

    /// <summary>
    /// Gets the outcome of the last update: scopes re-evaluated, scopes kept, total.
    /// </summary>
    public UpdateStats LastUpdate => _reconciler.LastStats;

    /// <summary>
    /// Gets the control of the root view, below the theme variant scope of Avalonia.
    /// </summary>
    internal Control? Content => _variant.Child;

    /// <inheritdoc/>
    public void RequestUpdate()
    {
        if (Interlocked.Exchange(ref _updatePending, 1) == 1)
        {
            return;
        }

        Dispatcher.UIThread.Post(Update);
    }

    /// <inheritdoc/>
    public void Report(Exception error)
    {
        if (UnhandledException is { } handler)
        {
            handler(error);
            return;
        }

        string message = $"[Reactor] Unhandled exception: {error}";
        Console.Error.WriteLine(message);
        Debug.WriteLine(message);
    }

    /// <inheritdoc/>
    public void Reload() => Dispatcher.UIThread.Post(() =>
    {
        _reconciler.MarkAllDirty();
        Interlocked.Exchange(ref _updatePending, 1);
        Update();
    });

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        Themes.Changed += _themesChanged;
        HotReload.Register(this);
        _window = TopLevel.GetTopLevel(this);
        _window?.AddHandler(KeyDownEvent, _keyDown, RoutingStrategies.Bubble);
        Dispatcher.UIThread.UnhandledException += _dispatcherError;

        // The light or dark mode of the system is followed as long as the window is open.
        _platform = this.GetPlatformSettings();
        if (_platform is not null)
        {
            Themes.SystemScheme = SchemeOf(_platform.GetColorValues());
            _platform.ColorValuesChanged += _systemColorsChanged;
        }

        Interlocked.Exchange(ref _updatePending, 1);
        Update();
    }

    /// <summary>
    /// Releases what the host subscribed to when the window closes (or the host is removed).
    /// </summary>
    /// <remarks>
    /// The scopes unsubscribe from their state, and the host from the theme catalog, hot reload, the platform settings,
    /// the dispatcher and the keyboard of the window; otherwise, state or a catalog that outlives the window would keep it
    /// in memory. Components keep their state.
    /// </remarks>
    /// <param name="e">The event data.</param>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        Themes.Changed -= _themesChanged;
        HotReload.Unregister(this);
        if (_platform is not null)
        {
            _platform.ColorValuesChanged -= _systemColorsChanged;
        }

        _window?.RemoveHandler(KeyDownEvent, _keyDown);
        Dispatcher.UIThread.UnhandledException -= _dispatcherError;
        _window = null;
        _platform = null;
        _reconciler.Release();
    }

    private static ColorScheme SchemeOf(PlatformColorValues colors) =>
        colors.ThemeVariant == PlatformThemeVariant.Dark ? ColorScheme.Dark : ColorScheme.Light;

    // Runs the action of a control on behalf of this host, then updates the interface.
    private void Run(Action? action)
    {
        if (action is null)
        {
            return;
        }

        try
        {
            RunningAction.Run(this, action);
        }
        catch (Exception exception)
        {
            Report(exception);
        }

        Update();
    }

    private void Update()
    {
        if (!_attached || Interlocked.Exchange(ref _updatePending, 0) == 0)
        {
            return;
        }

        Control content = _reconciler.Update(_root, Themes);
        if (!ReferenceEquals(_variant.Child, content))
        {
            Reconciler.Detach(content);
            _variant.Child = content;
        }

        if (_reconciler.RootTheme is not { } theme)
        {
            return;
        }

        this.SetBrush(BackgroundProperty, theme.Palette.Page);

        // What Fluent still draws (scroll bars, the window) follows the mode of the root view.
        _variant.RequestedThemeVariant = theme.Scheme == ColorScheme.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    // Window shortcuts, received after the focused control (a field keeps its editing keys): those of the menu bar, even
    // with the menus closed (the system menu bar, when shown, runs its own), then the zoom.
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || KeyMap.ToKeyCode(e.Key) is not { } key)
        {
            return;
        }

        Primitives.KeyModifiers modifiers = KeyMap.ToModifiers(e.KeyModifiers);
        e.Handled = MenuShortcut(key, modifiers) || ZoomShortcut(key, modifiers);
    }

    private bool MenuShortcut(KeyCode key, Primitives.KeyModifiers modifiers)
    {
        if (_reconciler.MenuBar is not { View: MenuBarView bar, Content: ReactorMenuBar { UsesSystemBar: false } })
        {
            return false;
        }

        bool editing = _window?.FocusManager?.GetFocusedElement() is TextBox;
        if (MenuShortcuts.Find(bar, key, modifiers, editing) is not { } item)
        {
            return false;
        }

        if (item.IsEnabled)
        {
            Run(item.Action);
        }

        return true;
    }

    private bool ZoomShortcut(KeyCode key, Primitives.KeyModifiers modifiers)
    {
        if (!ZoomShortcuts)
        {
            return false;
        }

        if (s_zoomInGestures.Any(g => g.Matches(key, modifiers)))
        {
            Zoom = Math.Round(Zoom + ZoomStep, 1);
        }
        else if (s_zoomOutGesture.Matches(key, modifiers))
        {
            Zoom = Math.Round(Zoom - ZoomStep, 1);
        }
        else if (s_zoomResetGesture.Matches(key, modifiers))
        {
            Zoom = 1;
        }
        else
        {
            return false;
        }

        return true;
    }

    // An exception thrown later on the user interface thread, outside Run: the continuation of an async void action,
    // for example. It is reported like an action error, without closing the application; with several windows, the
    // first host that sees it reports it.
    private void OnDispatcherError(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        e.Handled = true;
        Report(e.Exception);
    }
}
