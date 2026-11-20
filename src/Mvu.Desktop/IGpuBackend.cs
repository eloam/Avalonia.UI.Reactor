using SkiaSharp;

namespace Mvu.Desktop;

/// <summary>
/// Passage entre Skia et l'écran : l'API du GPU (Direct3D 12, OpenGL, plus tard Metal) qui reçoit le dessin de Skia
/// et l'affiche dans la fenêtre. Skia dessine la même image quel que soit le moteur.
/// </summary>
internal interface IGpuBackend : IDisposable
{
    /// <summary>Nom affiché dans les messages (« Direct3D 12 », « OpenGL »).</summary>
    string Name { get; }

    /// <summary>Surface Skia de l'image suivante, à la taille de la fenêtre en pixels ; null si elle n'a pas pu être créée.</summary>
    SKSurface? BeginFrame(int width, int height);

    /// <summary>Envoie le dessin au GPU et affiche l'image. Faux : le GPU a été perdu (pilote mis à jour, réinitialisé).</summary>
    bool EndFrame();
}
