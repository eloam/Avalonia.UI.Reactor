using Mvu.Primitives;
using Mvu.Theming;

namespace Mvu.Views;

/// <summary>
/// Description immuable d'un élément d'interface, produite par le <c>Body</c> d'un composant.
/// Le runtime en déduit layout, rendu et gestion des entrées. Aucun objet "contrôle" n'est retenu.
/// Les propriétés null prennent la valeur du thème actif.
/// </summary>
public abstract record View
{
    /// <summary>Identité stable (listes) : conserve focus/survol quand l'ordre change.</summary>
    public string? Key { get; init; }
    public Thickness? Padding { get; init; }
    /// <summary>null = taille naturelle, +∞ = remplit l'espace disponible.</summary>
    public float? Width { get; init; }
    public float? Height { get; init; }
    /// <summary>Part de l'espace restant sur l'axe principal du stack parent.</summary>
    public float Grow { get; init; }
    public Align? SelfAlign { get; init; }
    public ColorValue? Background { get; init; }
    public ColorValue? BorderColor { get; init; }
    public float? BorderWidth { get; init; }
    public float? CornerRadius { get; init; }
    // Styles de texte, hérités par les enfants.
    public ColorValue? Foreground { get; init; }
    public TextStyle? TextStyle { get; init; }
    /// <summary>Style de texte nommé du thème (<c>.Font("caption")</c>), prioritaire sur <see cref="TextStyle"/>.</summary>
    public string? TextStyleName { get; init; }
    public float? FontSize { get; init; }
    public bool? Bold { get; init; }
    /// <summary>Thème précis appliqué à cette vue et ses descendants (comme l'environnement SwiftUI).</summary>
    public Theme? Theme { get; init; }
    /// <summary>Famille (ou thème) du catalogue par son nom : <c>.Theme("Sable")</c>, clair ou sombre selon le mode.</summary>
    public string? ThemeName { get; init; }
    /// <summary>Famille de thèmes appliquée à cette branche, clair ou sombre selon le mode.</summary>
    public ThemeFamily? ThemeFamily { get; init; }
    /// <summary>Force clair/sombre pour cette branche : variante correspondante de la famille en cours.</summary>
    public ColorScheme? Scheme { get; init; }
    /// <summary>Action exécutée quand on clique sur la vue.</summary>
    public Action? TapAction { get; init; }
}
