using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Avalonia.UI.Reactor.Hosting;

using Avalonia.UI.Reactor.Reconciliation;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Contrôle Avalonia qui affiche un composant : il tient le réconciliateur et applique les mises à jour demandées
/// par les états. L'action d'un contrôle (un clic) est suivie tout de suite de sa mise à jour, pour que l'événement
/// suivant voie le nouvel état ; un changement venu d'ailleurs (tâche, autre thread) la planifie sur le thread d'interface.
/// </summary>
internal sealed class ReactorHost : Border, IScopeHost
{
    readonly View _root;
    readonly Reconciler _reconciler;
    readonly Action _themesChanged;
    int _updatePending = 1;
    bool _attached;

    public ReactorHost(Component root, Themes themes)
    {
        _root = root;
        Themes = themes;
        _reconciler = new Reconciler(this, Run);
        _themesChanged = RequestUpdate;
    }

    public Themes Themes { get; }

    /// <summary>Bilan de la dernière mise à jour : portées réévaluées, reprises telles quelles, total.</summary>
    public UpdateStats LastUpdate => _reconciler.LastStats;

    public void RequestUpdate()
    {
        if (Interlocked.Exchange(ref _updatePending, 1) == 1) return;
        Dispatcher.UIThread.Post(Update);
    }

    /// <summary>Exécute l'action d'un contrôle : une erreur est signalée sans fermer l'application, puis l'interface suit l'état.</summary>
    void Run(Action? action)
    {
        if (action is null) return;
        try { action(); }
        catch (Exception e) { Report(e); }
        Update();
    }

    void Update()
    {
        if (!_attached || Interlocked.Exchange(ref _updatePending, 0) == 0) return;
        var content = _reconciler.Update(_root, Themes);
        if (!ReferenceEquals(Child, content))
        {
            Reconciler.Detach(content);
            Child = content;
        }
        this.SetBrush(BackgroundProperty, _reconciler.RootTheme?.Palette.Page);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        Themes.Changed += _themesChanged;
        Interlocked.Exchange(ref _updatePending, 1);
        Update();
    }

    /// <summary>
    /// Fenêtre fermée (ou hôte retiré) : les portées se désabonnent de leurs états et l'hôte du catalogue de thèmes,
    /// sinon un état ou un catalogue qui survit à la fenêtre la garderait en mémoire. Les composants gardent leur état.
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        Themes.Changed -= _themesChanged;
        _reconciler.Dispose();
    }

    static void Report(Exception e)
    {
        Console.Error.WriteLine($"[Reactor] Unhandled exception: {e}");
        Debug.WriteLine($"[Reactor] Unhandled exception: {e}");
    }
}
