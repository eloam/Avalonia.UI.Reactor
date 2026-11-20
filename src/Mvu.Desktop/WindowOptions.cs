using Mvu.Theming;

namespace Mvu.Desktop;

/// <summary>
/// Réglages de la fenêtre. On ne précise que ce qui change :
/// <code>
/// Desktop.Run&lt;App&gt;(new WindowOptions { Title = "Mon app", Width = 1024, Themes = new(Theme.Light, sable) });
/// </code>
/// </summary>
public sealed record WindowOptions
{
    public string Title { get; init; } = "App";
    /// <summary>Taille de départ, en unités logiques (indépendantes du DPI).</summary>
    public int Width { get; init; } = 800;
    public int Height { get; init; } = 600;
    public int MinWidth { get; init; } = 320;
    public int MinHeight { get; init; } = 240;
    public bool Resizable { get; init; } = true;
    /// <summary>Fenêtre sans barre de titre ni bordure du système.</summary>
    public bool Borderless { get; init; }
    public bool Maximized { get; init; }
    public bool Fullscreen { get; init; }
    /// <summary>Synchronisation verticale (pas de déchirure ; limite le rendu à la fréquence de l'écran).</summary>
    public bool VSync { get; init; } = true;
    /// <summary>Thèmes proposés ; par défaut Clair et Sombre.</summary>
    public Themes? Themes { get; init; }
    /// <summary>Zoom de l'interface au démarrage (1 = 100 %), en plus du DPI de l'écran.</summary>
    public float Zoom { get; init; } = 1;
    /// <summary>Ctrl + / Ctrl − / Ctrl 0 pour zoomer, dézoomer, revenir à 100 %.</summary>
    public bool ZoomShortcuts { get; init; } = true;
    /// <summary>
    /// Barre de menus du <c>Body</c> : dessinée (<see cref="MenuBarMode.Custom"/>), celle du système
    /// (<see cref="MenuBarMode.Native"/>), ou selon la plateforme (<see cref="MenuBarMode.Auto"/> : système sur macOS,
    /// dessinée ailleurs). Les raccourcis fonctionnent dans tous les cas.
    /// </summary>
    public MenuBarMode MenuBar { get; init; } = MenuBarMode.Auto;
    /// <summary>API du GPU : Direct3D 12 sur Windows (OpenGL en repli), OpenGL ailleurs. Le rendu est identique.</summary>
    public GraphicsApi Graphics { get; init; } = GraphicsApi.Auto;
}
