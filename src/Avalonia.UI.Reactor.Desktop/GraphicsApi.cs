namespace Avalonia.UI.Reactor.Desktop;

/// <summary>API du GPU utilisée pour afficher le dessin de Skia. Le rendu est le même dans tous les cas.</summary>
public enum GraphicsApi
{
    /// <summary>Direct3D 12 sur Windows (OpenGL s'il n'est pas disponible), OpenGL ailleurs.</summary>
    Auto,
    Direct3D12,
    OpenGL,
}
