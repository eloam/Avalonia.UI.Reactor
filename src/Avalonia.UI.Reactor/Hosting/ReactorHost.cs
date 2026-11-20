using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
/// planifie sur le thread d'interface. Une erreur dans une action est signalée sans fermer l'application.
/// </summary>
internal sealed class ReactorHost : Border, IScopeHost
{
    readonly View _root;
    readonly Reconciler _reconciler;
    readonly Action _themesChanged;
    readonly EventHandler<PlatformColorValues> _systemColorsChanged;
    readonly EventHandler<KeyEventArgs> _keyDown;
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
        Child = _variant;
    }

    public Themes Themes { get; }

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
        _window = null;
        _platform = null;
        _reconciler.Dispose();
    }

    /// <summary>
    /// Raccourcis de la barre de menus, actifs dans toute la fenêtre même menus fermés : reçus après le contrôle qui a
    /// le focus (un champ garde ses touches d'édition). La barre du système, quand elle s'affiche, les exécute elle-même.
    /// </summary>
    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || _reconciler.MenuBar is not { View: MenuBarView bar, Content: ReactorMenuBar { UsesSystemBar: false } }) return;
        if (KeyMap.ToKeyCode(e.Key) is not { } key) return;
        bool editing = _window?.FocusManager?.GetFocusedElement() is TextBox;
        if (MenuShortcuts.Find(bar, key, KeyMap.ToModifiers(e.KeyModifiers), editing) is not { } item) return;
        e.Handled = true;
        if (item.IsEnabled) Run(item.Action);
    }

    static ColorScheme SchemeOf(PlatformColorValues colors) =>
        colors.ThemeVariant == PlatformThemeVariant.Dark ? ColorScheme.Dark : ColorScheme.Light;
}
