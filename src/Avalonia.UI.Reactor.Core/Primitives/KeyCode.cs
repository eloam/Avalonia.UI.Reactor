namespace Avalonia.UI.Reactor.Primitives;

/// <summary>Touches reconnues, indépendantes de la plateforme (l'hôte traduit ses propres codes).</summary>
public enum KeyCode
{
    Backspace, Delete, Left, Right, Home, End, Enter, Tab, Escape,
    Up, Down, PageUp, PageDown, Insert, Space,
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Plus, Minus, Equal, Comma, Period, Slash,
    /// <summary>La touche Alt seule : l'appuyer puis la relâcher active la barre de menus.</summary>
    Alt,
}
