namespace Avalonia.UI.Reactor.Primitives;

/// <summary>Touches de modification enfoncées avec une touche. <see cref="Meta"/> = Cmd sur macOS, Windows ailleurs.</summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1,
    Ctrl = 2,
    Alt = 4,
    Meta = 8,
}
