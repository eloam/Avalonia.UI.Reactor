// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Input;

namespace MartletUI.Hosting;

/// <summary>
/// Maps the keys and modifier keys of Avalonia to ours, for menu shortcuts.
/// </summary>
internal static class KeyMap
{
    /// <summary>
    /// Returns our key code for an Avalonia key.
    /// </summary>
    /// <param name="key">The Avalonia key.</param>
    /// <returns>Our key code, or <see langword="null"/> for a key that no shortcut can use.</returns>
    public static Primitives.KeyCode? ToKeyCode(Key key) => key switch
    {
        >= Key.A and <= Key.Z => Primitives.KeyCode.A + (key - Key.A),
        >= Key.D0 and <= Key.D9 => Primitives.KeyCode.D0 + (key - Key.D0),
        >= Key.NumPad0 and <= Key.NumPad9 => Primitives.KeyCode.D0 + (key - Key.NumPad0),
        >= Key.F1 and <= Key.F12 => Primitives.KeyCode.F1 + (key - Key.F1),
        Key.Back => Primitives.KeyCode.Backspace,
        Key.Delete => Primitives.KeyCode.Delete,
        Key.Left => Primitives.KeyCode.Left,
        Key.Right => Primitives.KeyCode.Right,
        Key.Up => Primitives.KeyCode.Up,
        Key.Down => Primitives.KeyCode.Down,
        Key.Home => Primitives.KeyCode.Home,
        Key.End => Primitives.KeyCode.End,
        Key.PageUp => Primitives.KeyCode.PageUp,
        Key.PageDown => Primitives.KeyCode.PageDown,
        Key.Insert => Primitives.KeyCode.Insert,
        Key.Enter => Primitives.KeyCode.Enter,
        Key.Tab => Primitives.KeyCode.Tab,
        Key.Escape => Primitives.KeyCode.Escape,
        Key.Space => Primitives.KeyCode.Space,
        Key.Add => Primitives.KeyCode.Plus,

        // The "= +" key of the main keyboard.
        Key.OemPlus => Primitives.KeyCode.Equal,
        Key.OemMinus or Key.Subtract => Primitives.KeyCode.Minus,
        Key.OemComma => Primitives.KeyCode.Comma,
        Key.OemPeriod or Key.Decimal => Primitives.KeyCode.Period,
        Key.OemQuestion or Key.Divide => Primitives.KeyCode.Slash,
        _ => null,
    };

    /// <summary>
    /// Returns the Avalonia key for our key code, to display a shortcut in a menu.
    /// </summary>
    /// <param name="code">Our key code.</param>
    /// <returns>The Avalonia key, or <see cref="Key.None"/> if there is none.</returns>
    public static Key ToKey(Primitives.KeyCode code) => code switch
    {
        >= Primitives.KeyCode.A and <= Primitives.KeyCode.Z => Key.A + (code - Primitives.KeyCode.A),
        >= Primitives.KeyCode.D0 and <= Primitives.KeyCode.D9 => Key.D0 + (code - Primitives.KeyCode.D0),
        >= Primitives.KeyCode.F1 and <= Primitives.KeyCode.F12 => Key.F1 + (code - Primitives.KeyCode.F1),
        Primitives.KeyCode.Backspace => Key.Back,
        Primitives.KeyCode.Delete => Key.Delete,
        Primitives.KeyCode.Left => Key.Left,
        Primitives.KeyCode.Right => Key.Right,
        Primitives.KeyCode.Up => Key.Up,
        Primitives.KeyCode.Down => Key.Down,
        Primitives.KeyCode.Home => Key.Home,
        Primitives.KeyCode.End => Key.End,
        Primitives.KeyCode.PageUp => Key.PageUp,
        Primitives.KeyCode.PageDown => Key.PageDown,
        Primitives.KeyCode.Insert => Key.Insert,
        Primitives.KeyCode.Enter => Key.Enter,
        Primitives.KeyCode.Tab => Key.Tab,
        Primitives.KeyCode.Escape => Key.Escape,
        Primitives.KeyCode.Space => Key.Space,
        Primitives.KeyCode.Plus => Key.Add,
        Primitives.KeyCode.Equal => Key.OemPlus,
        Primitives.KeyCode.Minus => Key.OemMinus,
        Primitives.KeyCode.Comma => Key.OemComma,
        Primitives.KeyCode.Period => Key.OemPeriod,
        Primitives.KeyCode.Slash => Key.OemQuestion,
        _ => Key.None,
    };

    /// <summary>
    /// Returns our modifier keys for Avalonia modifier keys.
    /// </summary>
    /// <param name="modifiers">The Avalonia modifier keys.</param>
    /// <returns>Our modifier keys.</returns>
    public static Primitives.KeyModifiers ToModifiers(KeyModifiers modifiers) =>
        (modifiers.HasFlag(KeyModifiers.Shift) ? Primitives.KeyModifiers.Shift : 0)
        | (modifiers.HasFlag(KeyModifiers.Control) ? Primitives.KeyModifiers.Ctrl : 0)
        | (modifiers.HasFlag(KeyModifiers.Alt) ? Primitives.KeyModifiers.Alt : 0)
        | (modifiers.HasFlag(KeyModifiers.Meta) ? Primitives.KeyModifiers.Meta : 0);

    /// <summary>
    /// Returns a shortcut as the user types it on this platform (Ctrl becomes Cmd on macOS).
    /// </summary>
    /// <param name="gesture">Our shortcut.</param>
    /// <returns>The Avalonia shortcut.</returns>
    public static KeyGesture ToGesture(Primitives.KeyGesture gesture)
    {
        Primitives.KeyModifiers effective = gesture.Effective;
        KeyModifiers modifiers = (effective.HasFlag(Primitives.KeyModifiers.Shift) ? KeyModifiers.Shift : 0)
                        | (effective.HasFlag(Primitives.KeyModifiers.Ctrl) ? KeyModifiers.Control : 0)
                        | (effective.HasFlag(Primitives.KeyModifiers.Alt) ? KeyModifiers.Alt : 0)
                        | (effective.HasFlag(Primitives.KeyModifiers.Meta) ? KeyModifiers.Meta : 0);
        return new KeyGesture(ToKey(gesture.Key), modifiers);
    }
}
