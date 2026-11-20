namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Hôte qui exécute l'action en cours sur ce thread (un clic, une saisie). Une action async le retient avant son premier
/// <c>await</c> pour lui signaler ses erreurs, quel que soit le contexte où sa suite s'exécute.
/// </summary>
internal static class RunningAction
{
    [ThreadStatic] static IScopeHost? _host;

    /// <summary>L'hôte de l'action en cours ; null hors d'une action.</summary>
    public static IScopeHost? Host => _host;

    /// <summary>Exécute une action de l'interface pour le compte de <paramref name="host"/>.</summary>
    public static void Run(IScopeHost host, Action action)
    {
        var previous = _host;
        _host = host;
        try { action(); }
        finally { _host = previous; }
    }
}
