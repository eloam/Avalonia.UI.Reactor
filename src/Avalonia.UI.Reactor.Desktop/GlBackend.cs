using SkiaSharp;
using SDL;
using static SDL.SDL3;

namespace Avalonia.UI.Reactor.Desktop;

/// <summary>Skia sur OpenGL 3.3, par SDL : macOS (en attendant Metal), Linux, et repli sur Windows.</summary>
internal sealed unsafe class GlBackend : IGpuBackend
{
    readonly SDL_Window* _window;
    readonly SDL_GLContextState* _gl;
    readonly GRGlInterface _interface;
    readonly GRContext _gr;
    GRBackendRenderTarget? _target;
    SKSurface? _surface;
    int _width, _height;

    public string Name => "OpenGL";

    /// <summary>Réglages à faire avant de créer la fenêtre (qui doit porter <c>SDL_WINDOW_OPENGL</c>).</summary>
    public static void PrepareWindow()
    {
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 3);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_CORE);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_DOUBLEBUFFER, 1);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_STENCIL_SIZE, 8);
    }

    public GlBackend(SDL_Window* window, bool vsync)
    {
        _window = window;
        _gl = SDL_GL_CreateContext(window);
        if (_gl == null) throw new InvalidOperationException($"Failed to create the OpenGL context: {SDL_GetError()}");
        SDL_GL_MakeCurrent(window, _gl);
        SDL_GL_SetSwapInterval(vsync ? 1 : 0);
        _interface = GRGlInterface.CreateOpenGl(name => SDL_GL_GetProcAddress(name))
                     ?? throw new InvalidOperationException("Failed to create the OpenGL interface.");
        _gr = GRContext.CreateGl(_interface) ?? throw new InvalidOperationException("Failed to create the Skia GPU context.");
    }

    public SKSurface? BeginFrame(int width, int height)
    {
        if (_surface is null || width != _width || height != _height)
        {
            _surface?.Dispose();
            _target?.Dispose();
            (_width, _height) = (width, height);
            const uint GL_RGBA8 = 0x8058;
            _target = new GRBackendRenderTarget(width, height, 0, 8, new GRGlFramebufferInfo(0, GL_RGBA8));
            _surface = SKSurface.Create(_gr, _target, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        }
        return _surface;
    }

    public bool EndFrame()
    {
        _gr.Flush();
        SDL_GL_SwapWindow(_window);
        return true;
    }

    public void Dispose()
    {
        _surface?.Dispose();
        _target?.Dispose();
        _gr.Dispose();
        _interface.Dispose();
        SDL_GL_DestroyContext(_gl);
    }
}
