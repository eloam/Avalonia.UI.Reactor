namespace Avalonia.UI.Reactor;

using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Réglages de la fenêtre. On ne précise que ce qui change :
/// <code>
/// ReactorApp.Run&lt;App&gt;(new WindowOptions { Title = "Mon app", Width = 1024, Themes = new(Theme.Light, sable) });
/// </code>
/// </summary>
public sealed record WindowOptions
{
    /// <summary>Titre de la fenêtre.</summary>
    public string Title { get; init; } = "App";

    /// <summary>Largeur de départ, en unités logiques (indépendantes du DPI).</summary>
    public int Width { get; init; } = 800;

    /// <summary>Hauteur de départ, en unités logiques.</summary>
    public int Height { get; init; } = 600;

    /// <summary>Largeur minimale.</summary>
    public int MinWidth { get; init; } = 320;

    /// <summary>Hauteur minimale.</summary>
    public int MinHeight { get; init; } = 240;

    /// <summary>L'utilisateur peut redimensionner la fenêtre.</summary>
    public bool Resizable { get; init; } = true;

    /// <summary>Fenêtre sans barre de titre ni bordure du système.</summary>
    public bool Borderless { get; init; }

    /// <summary>Fenêtre agrandie au démarrage.</summary>
    public bool Maximized { get; init; }

    /// <summary>Plein écran au démarrage.</summary>
    public bool Fullscreen { get; init; }

    /// <summary>Thèmes proposés ; par défaut Clair et Sombre.</summary>
    public Themes? Themes { get; init; }
}
