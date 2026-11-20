using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Ce qui affiche des portées (une fenêtre, quel que soit son moteur) : il reçoit leurs demandes de mise à jour,
/// fournit le catalogue de thèmes aux composants qu'il affiche et signale les erreurs de leurs actions.
/// </summary>
internal interface IScopeHost
{
    /// <summary>Catalogue des thèmes de l'application (<c>Component.Themes</c>).</summary>
    Themes Themes { get; }

    /// <summary>Un état lu par une portée a changé : planifier une mise à jour. Appelable depuis n'importe quel thread.</summary>
    void RequestUpdate();

    /// <summary>Erreur d'une action, synchrone ou async : elle est signalée et l'application continue.</summary>
    void Report(Exception error);

    /// <summary>Hot reload : tout réévaluer sur le thread d'interface de l'hôte. Appelable depuis n'importe quel thread.</summary>
    void Reload();
}
