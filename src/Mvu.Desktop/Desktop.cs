using Mvu.State;
using Mvu.Theming;
using SDL;
using static SDL.SDL3;

namespace Mvu.Desktop;

public static class Desktop
{
    /// <summary>Ouvre <typeparamref name="TApp"/> dans une fenêtre native (SDL3 + Skia GPU).</summary>
    public static void Run<TApp>(string title = "App", int width = 800, int height = 600, Themes? themes = null)
        where TApp : Component, new() =>
        Run(new TApp(), new WindowOptions { Title = title, Width = width, Height = height, Themes = themes });

    /// <summary>Même chose, avec tous les réglages de la fenêtre.</summary>
    public static void Run<TApp>(WindowOptions options) where TApp : Component, new() => Run(new TApp(), options);

    public static void Run(Component app, WindowOptions? options = null)
    {
        using var window = new SdlWindow(app, options ?? new WindowOptions());
        window.Run();
    }

    /// <summary>Ferme la fenêtre et termine <see cref="Run(Component, WindowOptions?)"/> (menu « Quitter », par exemple).</summary>
    public static unsafe void Quit()
    {
        SDL_Event e = default;
        e.type = (uint)SDL_EventType.SDL_EVENT_QUIT;
        SDL_PushEvent(&e);
    }
}
