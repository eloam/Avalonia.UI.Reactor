using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Ce qui affiche des portées (une fenêtre, quel que soit son moteur) : il reçoit leurs demandes de mise à jour
/// et fournit le catalogue de thèmes aux composants qu'il affiche.
/// </summary>
internal interface IScopeHost
{
    /// <summary>Catalogue des thèmes de l'application (<c>Component.Themes</c>).</summary>
    Themes Themes { get; }

    /// <summary>Un état lu par une portée a changé : planifier une mise à jour. Appelable depuis n'importe quel thread.</summary>
    void RequestUpdate();
}
