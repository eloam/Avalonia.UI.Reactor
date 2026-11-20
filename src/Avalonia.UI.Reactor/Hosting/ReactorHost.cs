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
using Avalonia.UI.Reactor.Reconciliation;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Contrôle Avalonia qui affiche un composant : il tient le réconciliateur et applique les mises à jour demandées
/// par les états. L'action d'un contrôle (un clic, une saisie) est suivie tout de suite de sa mise à jour, pour que
/// l'événement suivant voie le nouvel état ; un changement venu d'ailleurs (suite d'un <c>await</c>, autre thread) la
/// planifie sur le thread d'interface. Une erreur dans une action, ou plus tard sur le thread
/// d'interface (suite d'un <c>async void</c>), est signalée sans fermer l'application. L'hôte applique aussi le zoom de
/// l'interface (Ctrl + / Ctrl − / Ctrl 0, Cmd sur macOS).
/// </summary>
internal sealed class ReactorHost : Border, IScopeHost
{
    /// <summary>Plus petit zoom de l'interface (50 %).</summary>
    internal const double MinZoom = 0.5;

    /// <summary>Plus grand zoom de l'interface (300 %).</summary>
    internal const double MaxZoom = 3;

    /// <summary>Pas des raccourcis de zoom (10 %).</summary>
    internal const double ZoomStep = 0.1;

    static readonly Primitives.KeyGesture[] ZoomInGestures =
        [new(Primitives.KeyCode.Plus, Primitives.KeyModifiers.Ctrl), new(Primitives.KeyCode.Equal, Primitives.KeyModifiers.Ctrl)];
    static readonly Primitives.KeyGesture ZoomOutGesture = new(Primitives.KeyCode.Minus, Primitives.KeyModifiers.Ctrl);
    static readonly Primitives.KeyGesture ZoomResetGesture = new(Primitives.KeyCode.D0, Primitives.KeyModifiers.Ctrl);

    readonly View _root;
    readonly Reconciler _reconciler;
    readonly Action _themesChanged;
    readonly EventHandler<PlatformColorValues> _systemColorsChanged;
    readonly EventHandler<KeyEventArgs> _keyDown;
    readonly DispatcherUnhandledExceptionEventHandler _dispatcherError;
    readonly LayoutTransformControl _zoom = new();
    readonly ThemeVariantScope _variant = new();
    IPlatformSettings? _platform;
    TopLevel? _window;
    int _updatePending = 1;
    bool _attached;

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

    public Themes Themes { get; }

    /// <summary>
    /// Zoom de l'interface (1 = 100 %, de <see cref="MinZoom"/> à <see cref="MaxZoom"/>), en plus du facteur d'échelle
    /// de l'écran : la mise en page se refait à la nouvelle taille, le texte reste net.
    /// </summary>
    public double Zoom
    {
        get => _zoomFactor;
        set
        {
            double zoom = Math.Clamp(value, MinZoom, MaxZoom);
            if (zoom == _zoomFactor) return;
            _zoomFactor = zoom;
            _zoom.LayoutTransform = zoom == 1 ? null : new ScaleTransform(zoom, zoom);
        }
    }
    double _zoomFactor = 1;

    /// <summary>Ctrl + / Ctrl − / Ctrl 0 (Cmd sur macOS) zooment, dézooment, reviennent à 100 % ; vrai par défaut.</summary>
    public bool ZoomShortcuts { get; set; } = true;

    /// <summary>Contrôle de la vue racine (sous la portée de variante d'Avalonia).</summary>
    internal Control? Content => _variant.Child;

    /// <summary>Bilan de la dernière mise à jour : portées réévaluées, reprises telles quelles, total.</summary>
    public UpdateStats LastUpdate => _reconciler.LastStats;

    /// <summary>Erreur dans une action (synchrone ou async) ; sans abonné, elle est écrite sur la sortie d'erreur et dans le débogueur.</summary>
    public event Action<Exception>? UnhandledException;

    public void RequestUpdate()
    {
        if (Interlocked.Exchange(ref _updatePending, 1) == 1) return;
        Dispatcher.UIThread.Post(Update);
    }

    public void Report(Exception error)
    {
        if (UnhandledException is { } handler) handler(error);
        else
        {
            Console.Error.WriteLine($"[Reactor] Unhandled exception: {error}");
            Debug.WriteLine($"[Reactor] Unhandled exception: {error}");
        }
    }

    public void Reload() => Dispatcher.UIThread.Post(() =>
    {
        _reconciler.MarkAllDirty();
        Interlocked.Exchange(ref _updatePending, 1);
        Update();
    });

    /// <summary>Exécute l'action d'un contrôle pour le compte de cet hôte, puis met l'interface à jour.</summary>
    void Run(Action? action)
    {
        if (action is null) return;
        try { RunningAction.Run(this, action); }
        catch (Exception e) { Report(e); }
        Update();
    }

    void Update()
    {
        if (!_attached || Interlocked.Exchange(ref _updatePending, 0) == 0) return;
        var content = _reconciler.Update(_root, Themes);
        if (!ReferenceEquals(_variant.Child, content))
        {
            Reconciler.Detach(content);
            _variant.Child = content;
        }
        if (_reconciler.RootTheme is not { } theme) return;
        this.SetBrush(BackgroundProperty, theme.Palette.Page);
        // Ce que Fluent dessine encore (défilement, fenêtre) suit le mode de la vue racine.
        _variant.RequestedThemeVariant = theme.Scheme == ColorScheme.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        Themes.Changed += _themesChanged;
        HotReload.Register(this);
        _window = TopLevel.GetTopLevel(this);
        _window?.AddHandler(KeyDownEvent, _keyDown, RoutingStrategies.Bubble);
        Dispatcher.UIThread.UnhandledException += _dispatcherError;
        // Mode clair ou sombre du système, suivi tant que la fenêtre est ouverte.
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
    /// Fenêtre fermée (ou hôte retiré) : les portées se désabonnent de leurs états et l'hôte du catalogue de thèmes et du
    /// hot reload, sinon un état ou un catalogue qui survit à la fenêtre la garderait en mémoire. Les composants gardent leur état.
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        Themes.Changed -= _themesChanged;
        HotReload.Unregister(this);
        if (_platform is not null) _platform.ColorValuesChanged -= _systemColorsChanged;
        _window?.RemoveHandler(KeyDownEvent, _keyDown);
        Dispatcher.UIThread.UnhandledException -= _dispatcherError;
        _window = null;
        _platform = null;
        _reconciler.Dispose();
    }

    /// <summary>
    /// Raccourcis de la fenêtre, reçus après le contrôle qui a le focus (un champ garde ses touches d'édition) : ceux de
    /// la barre de menus, même menus fermés (la barre du système, quand elle s'affiche, exécute elle-même les siens),
    /// puis le zoom.
    /// </summary>
    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || KeyMap.ToKeyCode(e.Key) is not { } key) return;
        var modifiers = KeyMap.ToModifiers(e.KeyModifiers);
        e.Handled = MenuShortcut(key, modifiers) || ZoomShortcut(key, modifiers);
    }

    bool MenuShortcut(Primitives.KeyCode key, Primitives.KeyModifiers modifiers)
    {
        if (_reconciler.MenuBar is not { View: MenuBarView bar, Content: ReactorMenuBar { UsesSystemBar: false } }) return false;
        bool editing = _window?.FocusManager?.GetFocusedElement() is TextBox;
        if (MenuShortcuts.Find(bar, key, modifiers, editing) is not { } item) return false;
        if (item.IsEnabled) Run(item.Action);
        return true;
    }

    bool ZoomShortcut(Primitives.KeyCode key, Primitives.KeyModifiers modifiers)
    {
        if (!ZoomShortcuts) return false;
        if (ZoomInGestures.Any(g => g.Matches(key, modifiers))) Zoom = Math.Round(Zoom + ZoomStep, 1);
        else if (ZoomOutGesture.Matches(key, modifiers)) Zoom = Math.Round(Zoom - ZoomStep, 1);
        else if (ZoomResetGesture.Matches(key, modifiers)) Zoom = 1;
        else return false;
        return true;
    }

    /// <summary>
    /// Exception levée plus tard sur le thread d'interface, hors de <see cref="Run"/> : la suite d'une action
    /// <c>async void</c>, par exemple. Elle est signalée comme une erreur d'action, sans fermer l'application ; avec
    /// plusieurs fenêtres, le premier hôte qui la voit la signale.
    /// </summary>
    void OnDispatcherError(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Handled) return;
        e.Handled = true;
        Report(e.Exception);
    }

    static ColorScheme SchemeOf(PlatformColorValues colors) =>
        colors.ThemeVariant == PlatformThemeVariant.Dark ? ColorScheme.Dark : ColorScheme.Light;
}
