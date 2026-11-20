using Avalonia.UI.Reactor.Layout;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Portée de recalcul : un composant (ou un élément de <c>ForEach</c>). Elle retient les états lus
/// lors de son dernier calcul ; seul un changement de l'un d'eux la refait calculer.
/// Garde aussi le résultat du layout pour le réutiliser tant que rien n'a changé dessous.
/// </summary>
internal sealed class Scope(IScopeHost host, string id, object? identity)
{
    [ThreadStatic] internal static Scope? Current;

    readonly HashSet<Signal> _deps = [];
    volatile bool _dirty = true;

    public string Id { get; } = id;
    public object? Identity { get; } = identity;

    // Résultat du dernier calcul.
    public View? Content;
    public ScopeView? LastView;
    // Cache de layout.
    public LayoutNode? Node;
    public object? EnvKey;
    public List<Scope> Descendants = [];
    public int LastVisit;

    public bool IsDirty => _dirty;

    internal void Track(Signal s)
    {
        lock (_deps) _deps.Add(s);
    }

    /// <summary>Appelable depuis n'importe quel thread.</summary>
    public void MarkDirty()
    {
        _dirty = true;
        host.RequestUpdate();
    }

    /// <summary>Recalcule le contenu en enregistrant les états lus.</summary>
    public View Evaluate(Func<View> build)
    {
        _dirty = false; // avant le calcul : un changement concurrent la remettra à recalculer
        ClearDeps();
        var previous = Current;
        Current = this;
        try { return build(); }
        finally { Current = previous; }
    }

    public void Dispose() => ClearDeps();

    void ClearDeps()
    {
        Signal[] deps;
        lock (_deps) { deps = [.. _deps]; _deps.Clear(); }
        foreach (var d in deps) d.Unsubscribe(this);
    }
}
