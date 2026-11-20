using Mvu.Primitives;
using Mvu.Runtime;
using Mvu.Skia;
using Mvu.State;
using Mvu.Theming;
using SkiaSharp;
using SDL;
using Windows.Win32.Foundation;
using static SDL.SDL3;

namespace Mvu.Desktop;

/// <summary>
/// Hôte de fenêtre : SDL3 fournit la fenêtre, les événements et les services du système (presse-papiers) ;
/// un moteur GPU (<see cref="IGpuBackend"/>) affiche le dessin de Skia. Toute la logique d'UI vit dans <see cref="UiHost"/>.
/// </summary>
internal sealed unsafe class SdlWindow : IDisposable
{
    readonly WindowOptions _options;
    readonly SDL_Window* _window;
    readonly SkiaDrawingContext _ctx = new();
    readonly UiHost _ui;
    readonly SDL_Cursor* _arrow, _hand, _ibeam;
    readonly INativeMenuBar? _nativeMenu;
    IGpuBackend _gpu;
    bool _dirty = true;
    CursorKind _cursor;

    public SdlWindow(Component app, WindowOptions options)
    {
        _options = options;
        bool nativeMenu = WantsNativeMenu(options);
        if (nativeMenu && OperatingSystem.IsWindows())
            SDL_SetHint(SDL_HINT_WINDOWS_ENABLE_MENU_MNEMONICS, "1"); // Alt et F10 ouvrent la barre du système
        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO)) throw new InvalidOperationException(SDL_GetError());

        _gpu = CreateWindowAndGpu(options, out _window);

        _arrow = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT);
        _hand = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER);
        _ibeam = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT);

        _ui = new UiHost(app, _ctx, options.Themes, wake: Wake)
        {
            SystemScheme = ReadSystemScheme(),
            Zoom = options.Zoom,
            ZoomShortcuts = options.ZoomShortcuts,
            Clipboard = new SdlClipboard(),
        };
        _ui.InvalidateRequested += () => _dirty = true;

        if (nativeMenu) _nativeMenu = CreateNativeMenu();
        if (_nativeMenu is not null)
        {
            _ui.NativeMenuBar = _nativeMenu;
            Render(); // première mise en page : la barre du système est attachée avant le dimensionnement
        }

        // Taille demandée en unités logiques (zoom compris) → coordonnées fenêtre de la plateforme
        // (SDL compte la barre de menus du système en dehors de cette taille).
        float toWindow = SDL_GetWindowDisplayScale(_window) * options.Zoom / SDL_GetWindowPixelDensity(_window);
        SDL_SetWindowSize(_window, (int)(options.Width * toWindow), (int)(options.Height * toWindow));
        SDL_SetWindowMinimumSize(_window, (int)(options.MinWidth * toWindow), (int)(options.MinHeight * toWindow));

        SDL_StartTextInput(_window);
        SDL_ShowWindow(_window);
    }

    /// <summary>
    /// Fenêtre et moteur GPU : Direct3D 12 sur Windows, OpenGL ailleurs ou en repli (la fenêtre est alors recréée,
    /// car OpenGL se choisit à sa création).
    /// </summary>
    static IGpuBackend CreateWindowAndGpu(WindowOptions options, out SDL_Window* window)
    {
        if (options.Graphics != GraphicsApi.OpenGL && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
        {
            window = CreateWindow(options, opengl: false);
            if (TryCreateDirect3D(window, options.VSync) is { } direct3D) return direct3D;
            SDL_DestroyWindow(window);
        }
        else if (options.Graphics == GraphicsApi.Direct3D12)
            Console.Error.WriteLine("[Mvu] Direct3D 12 requires Windows 10 or later; using OpenGL.");

        GlBackend.PrepareWindow();
        window = CreateWindow(options, opengl: true);
        return new GlBackend(window, options.VSync);
    }

    /// <summary>Moteur Direct3D 12 sur cette fenêtre ; null (avec un message) si la machine ne le permet pas.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240")]
    static D3D12Backend? TryCreateDirect3D(SDL_Window* window, bool vsync)
    {
        int width, height;
        SDL_GetWindowSizeInPixels(window, &width, &height);
        try
        {
            return new D3D12Backend(Win32HandleOf(window), Math.Max(1, width), Math.Max(1, height), vsync);
        }
        catch (Exception e) // tout échec d'initialisation (pilote, carte trop ancienne, bureau à distance…) mène au repli
        {
            Console.Error.WriteLine($"[Mvu] Direct3D 12 is not available ({e.Message}); using OpenGL.");
            return null;
        }
    }

    static SDL_Window* CreateWindow(WindowOptions options, bool opengl)
    {
        var flags = SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY | SDL_WindowFlags.SDL_WINDOW_HIDDEN;
        if (opengl) flags |= SDL_WindowFlags.SDL_WINDOW_OPENGL;
        if (options.Resizable) flags |= SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
        if (options.Borderless) flags |= SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
        if (options.Maximized) flags |= SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;
        if (options.Fullscreen) flags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
        var window = SDL_CreateWindow(options.Title, options.Width, options.Height, flags);
        if (window == null) throw new InvalidOperationException(SDL_GetError());
        return window;
    }

    /// <summary>Handle Win32 de la fenêtre SDL.</summary>
    static HWND Win32HandleOf(SDL_Window* window) =>
        new(SDL_GetPointerProperty(SDL_GetWindowProperties(window), SDL_PROP_WINDOW_WIN32_HWND_POINTER, IntPtr.Zero));

    /// <summary>Réveille la boucle d'événements (appelable depuis n'importe quel thread).</summary>
    static void Wake()
    {
        SDL_Event e = default;
        e.type = (uint)SDL_EventType.SDL_EVENT_USER;
        SDL_PushEvent(&e);
    }

    /// <summary>Barre du système voulue : réglage explicite, sinon celle de macOS.</summary>
    static bool WantsNativeMenu(WindowOptions options) => options.MenuBar switch
    {
        MenuBarMode.Native => true,
        MenuBarMode.Custom => false,
        _ => OperatingSystem.IsMacOS(),
    };

    /// <summary>Barre du système de la plateforme ; null (barre dessinée) là où elle n'existe pas encore.</summary>
    INativeMenuBar? CreateNativeMenu()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 0, 6000))
            return new Win32MenuBar(Win32HandleOf(_window), action => _ui.Dispatcher.Post(_ => action(), null));
        Console.Error.WriteLine("[Mvu] A native menu bar is not available on this platform yet; using the custom menu bar.");
        return null;
    }

    static ColorScheme ReadSystemScheme() =>
        SDL_GetSystemTheme() == SDL_SystemTheme.SDL_SYSTEM_THEME_DARK ? ColorScheme.Dark : ColorScheme.Light;

    /// <summary>Pixels physiques par unité logique : DPI de l'écran × zoom de l'interface.</summary>
    float Scale => SDL_GetWindowDisplayScale(_window) * _ui.Zoom;

    /// <summary>Coordonnées fenêtre SDL → unités logiques.</summary>
    Point ToLogical(float x, float y)
    {
        float f = SDL_GetWindowPixelDensity(_window) / Scale;
        return new Point(x * f, y * f);
    }

    public void Run()
    {
        SynchronizationContext.SetSynchronizationContext(_ui.Dispatcher);
        SDL_Event ev;
        long nextBlink = 0;
        while (true)
        {
            if (_dirty) Render();

            // Curseur de texte : clignotement réglé par le thème. Sans champ actif, la boucle dort
            // jusqu'au prochain événement (aucune consommation au repos).
            int blinkMs = _ui.IsEditingText ? _ui.CurrentTheme.TextField.CaretBlinkMs : 0;
            if (blinkMs > 0 && nextBlink == 0) nextBlink = Environment.TickCount64 + blinkMs;
            if (blinkMs <= 0) nextBlink = 0;

            bool gotEvent = blinkMs > 0
                ? SDL_WaitEventTimeout(&ev, (int)Math.Max(0, nextBlink - Environment.TickCount64))
                : SDL_WaitEvent(&ev);
            if (gotEvent)
            {
                do { if (!Handle(ref ev)) return; } while (SDL_PollEvent(&ev));
            }
            _ui.Dispatcher.Drain();
            if (nextBlink != 0 && Environment.TickCount64 >= nextBlink)
            {
                _ui.BlinkCaret();
                nextBlink = Environment.TickCount64 + blinkMs;
            }
        }
    }

    bool Handle(ref SDL_Event ev)
    {
        switch (ev.Type)
        {
            case SDL_EventType.SDL_EVENT_QUIT:
                return false;

            case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
            case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
            case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
            case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED:
                _dirty = true;
                break;

            case SDL_EventType.SDL_EVENT_SYSTEM_THEME_CHANGED:
                _ui.SystemScheme = ReadSystemScheme();
                break;

            case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                _ui.PointerMove(ToLogical(ev.motion.x, ev.motion.y));
                UpdateCursor();
                break;

            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                _ui.PointerLeave();
                break;

            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN when ev.button.button == SDL_BUTTON_LEFT:
                _ui.PointerDown(ToLogical(ev.button.x, ev.button.y));
                break;

            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP when ev.button.button == SDL_BUTTON_LEFT:
                _ui.PointerUp(ToLogical(ev.button.x, ev.button.y));
                break;

            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                if (ev.text.GetText() is { Length: > 0 } text) _ui.TextInput(text);
                break;

            case SDL_EventType.SDL_EVENT_KEY_DOWN:
                // Alt maintenu : un seul appui (Alt seul, relâché, active la barre de menus).
                if (KeyOf(ev.key.key) is { } pressed && !(pressed == KeyCode.Alt && ev.key.repeat))
                    _ui.KeyDown(pressed, ModifiersOf(ev.key.mod));
                break;

            case SDL_EventType.SDL_EVENT_KEY_UP:
                if (KeyOf(ev.key.key) is { } released) _ui.KeyUp(released, ModifiersOf(ev.key.mod));
                break;

            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                _ui.WindowDeactivated();
                break;
        }
        return true;
    }

    static KeyModifiers ModifiersOf(SDL_Keymod mod) =>
        ((mod & SDL_Keymod.SDL_KMOD_SHIFT) != 0 ? KeyModifiers.Shift : 0)
        | ((mod & SDL_Keymod.SDL_KMOD_CTRL) != 0 ? KeyModifiers.Ctrl : 0)
        | ((mod & SDL_Keymod.SDL_KMOD_ALT) != 0 ? KeyModifiers.Alt : 0)
        | ((mod & SDL_Keymod.SDL_KMOD_GUI) != 0 ? KeyModifiers.Meta : 0);

    /// <summary>Touche SDL (selon la disposition du clavier) → touche du framework ; null si elle n'a pas d'équivalent.</summary>
    static KeyCode? KeyOf(SDL_Keycode key) => key switch
    {
        >= SDL_Keycode.SDLK_A and <= SDL_Keycode.SDLK_Z => KeyCode.A + (int)(key - SDL_Keycode.SDLK_A),
        >= SDL_Keycode.SDLK_0 and <= SDL_Keycode.SDLK_9 => KeyCode.D0 + (int)(key - SDL_Keycode.SDLK_0),
        >= SDL_Keycode.SDLK_F1 and <= SDL_Keycode.SDLK_F12 => KeyCode.F1 + (int)(key - SDL_Keycode.SDLK_F1),
        SDL_Keycode.SDLK_KP_0 => KeyCode.D0,
        SDL_Keycode.SDLK_BACKSPACE => KeyCode.Backspace,
        SDL_Keycode.SDLK_DELETE => KeyCode.Delete,
        SDL_Keycode.SDLK_LEFT => KeyCode.Left,
        SDL_Keycode.SDLK_RIGHT => KeyCode.Right,
        SDL_Keycode.SDLK_UP => KeyCode.Up,
        SDL_Keycode.SDLK_DOWN => KeyCode.Down,
        SDL_Keycode.SDLK_HOME => KeyCode.Home,
        SDL_Keycode.SDLK_END => KeyCode.End,
        SDL_Keycode.SDLK_PAGEUP => KeyCode.PageUp,
        SDL_Keycode.SDLK_PAGEDOWN => KeyCode.PageDown,
        SDL_Keycode.SDLK_INSERT => KeyCode.Insert,
        SDL_Keycode.SDLK_RETURN or SDL_Keycode.SDLK_KP_ENTER => KeyCode.Enter,
        SDL_Keycode.SDLK_TAB => KeyCode.Tab,
        SDL_Keycode.SDLK_ESCAPE => KeyCode.Escape,
        SDL_Keycode.SDLK_SPACE => KeyCode.Space,
        SDL_Keycode.SDLK_PLUS or SDL_Keycode.SDLK_KP_PLUS => KeyCode.Plus,
        SDL_Keycode.SDLK_MINUS or SDL_Keycode.SDLK_KP_MINUS => KeyCode.Minus,
        SDL_Keycode.SDLK_EQUALS => KeyCode.Equal,
        SDL_Keycode.SDLK_COMMA => KeyCode.Comma,
        SDL_Keycode.SDLK_PERIOD => KeyCode.Period,
        SDL_Keycode.SDLK_SLASH => KeyCode.Slash,
        SDL_Keycode.SDLK_LALT or SDL_Keycode.SDLK_RALT => KeyCode.Alt,
        _ => null,
    };

    void UpdateCursor()
    {
        if (_ui.Cursor == _cursor) return;
        _cursor = _ui.Cursor;
        SDL_SetCursor(_cursor switch { CursorKind.Hand => _hand, CursorKind.IBeam => _ibeam, _ => _arrow });
    }

    void Render()
    {
        _dirty = false;
        int pw, ph;
        SDL_GetWindowSizeInPixels(_window, &pw, &ph);
        if (pw <= 0 || ph <= 0) return;

        if (_gpu.BeginFrame(pw, ph) is not { } surface) return;

        float scale = Scale;
        var canvas = surface.Canvas;
        var bg = _ui.ClearColor;
        canvas.Clear(new SKColor(bg.R, bg.G, bg.B));
        _ctx.Begin(canvas, scale);
        _ui.Paint(_ctx, new Size(pw / scale, ph / scale));
        if (!_gpu.EndFrame()) RecreateGpu();
    }

    /// <summary>GPU perdu (pilote mis à jour ou réinitialisé) : nouveau moteur du même type sur la même fenêtre.</summary>
    void RecreateGpu()
    {
        Console.Error.WriteLine($"[Mvu] The GPU device was lost; recreating the {_gpu.Name} renderer.");
        bool direct3D = _gpu is D3D12Backend;
        _gpu.Dispose();
        // La fenêtre a été créée pour ce moteur (avec ou sans OpenGL) : on recrée le même.
        _gpu = !direct3D ? new GlBackend(_window, _options.VSync)
            : (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ? TryCreateDirect3D(_window, _options.VSync) : null)
              ?? throw new InvalidOperationException("The GPU device was lost and Direct3D 12 could not be recreated.");
        _dirty = true;
    }

    public void Dispose()
    {
        (_nativeMenu as IDisposable)?.Dispose();
        _ui.Dispose();
        _gpu.Dispose();
        _ctx.Dispose();
        SDL_DestroyCursor(_arrow);
        SDL_DestroyCursor(_hand);
        SDL_DestroyCursor(_ibeam);
        SDL_DestroyWindow(_window);
        SDL_Quit();
    }
}
