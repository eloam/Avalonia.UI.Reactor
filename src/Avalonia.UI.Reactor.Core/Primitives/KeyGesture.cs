// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Primitives;

/// <summary>
/// Represents a keyboard shortcut: a key and its modifier keys.
/// </summary>
/// <remarks>
/// A shortcut is written as it reads: <c>KeyGesture.Parse("Ctrl+Shift+S")</c>, <c>"F5"</c>, <c>"Ctrl+="</c>. On macOS,
/// <c>Ctrl</c> stands for the Command key, as the conventions of the system require.
/// </remarks>
/// <param name="Key">The key.</param>
/// <param name="Modifiers">The modifier keys, as written: <see cref="KeyModifiers.Ctrl"/> stands for the command key.</param>
public readonly record struct KeyGesture(KeyCode Key, KeyModifiers Modifiers = KeyModifiers.None)
{
    private static readonly Dictionary<string, KeyCode> s_names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = KeyCode.Enter,
        ["Return"] = KeyCode.Enter,
        ["Esc"] = KeyCode.Escape,
        ["Escape"] = KeyCode.Escape,
        ["Tab"] = KeyCode.Tab,
        ["Space"] = KeyCode.Space,
        ["Backspace"] = KeyCode.Backspace,
        ["Del"] = KeyCode.Delete,
        ["Delete"] = KeyCode.Delete,
        ["Ins"] = KeyCode.Insert,
        ["Insert"] = KeyCode.Insert,
        ["Home"] = KeyCode.Home,
        ["End"] = KeyCode.End,
        ["PgUp"] = KeyCode.PageUp,
        ["PageUp"] = KeyCode.PageUp,
        ["PgDn"] = KeyCode.PageDown,
        ["PageDown"] = KeyCode.PageDown,
        ["Up"] = KeyCode.Up,
        ["Down"] = KeyCode.Down,
        ["Left"] = KeyCode.Left,
        ["Right"] = KeyCode.Right,
        ["Plus"] = KeyCode.Plus,
        ["+"] = KeyCode.Plus,
        ["Minus"] = KeyCode.Minus,
        ["-"] = KeyCode.Minus,
        ["="] = KeyCode.Equal,
        [","] = KeyCode.Comma,
        ["."] = KeyCode.Period,
        ["/"] = KeyCode.Slash,
    };

    /// <summary>
    /// Parses a shortcut written as <c>"Ctrl+N"</c>, <c>"Ctrl+Shift+S"</c>, <c>"Alt+F4"</c> or <c>"F5"</c>.
    /// </summary>
    /// <param name="text">The shortcut, with its modifiers and its key separated by <c>+</c>.</param>
    /// <returns>The parsed shortcut.</returns>
    /// <exception cref="ArgumentException"><paramref name="text"/> is not a valid shortcut.</exception>
    public static KeyGesture Parse(string text)
    {
        // In "Ctrl++", the last "+" is the key, not a separator.
        string[] parts = text.EndsWith("++", StringComparison.Ordinal)
            ? [.. text[..^2].Split('+', StringSplitOptions.TrimEntries), "+"]
            : text.Split('+', StringSplitOptions.TrimEntries);

        KeyModifiers modifiers = KeyModifiers.None;
        foreach (string part in parts[..^1])
        {
            modifiers |= part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => KeyModifiers.Ctrl,
                "shift" => KeyModifiers.Shift,
                "alt" or "option" => KeyModifiers.Alt,
                "cmd" or "command" or "meta" or "win" or "super" => KeyModifiers.Meta,
                _ => throw Invalid(text),
            };
        }
        return new KeyGesture(ParseKey(parts[^1]) ?? throw Invalid(text), modifiers);
    }

    private static KeyCode? ParseKey(string key)
    {
        if (key.Length == 1 && char.IsAsciiLetter(key[0]))
        {
            return KeyCode.A + (char.ToUpperInvariant(key[0]) - 'A');
        }

        if (key.Length == 1 && char.IsAsciiDigit(key[0]))
        {
            return KeyCode.D0 + (key[0] - '0');
        }

        if (key.Length is 2 or 3 && (key[0] is 'F' or 'f') && int.TryParse(key[1..], out int number) && number is >= 1 and <= 12)
        {
            return KeyCode.F1 + (number - 1);
        }

        return s_names.TryGetValue(key, out KeyCode code) ? code : null;
    }

    private static ArgumentException Invalid(string text) =>
        new($"'{text}' is not a valid keyboard shortcut. Use a form such as 'Ctrl+N', 'Ctrl+Shift+S' or 'F5'.", nameof(text));

    /// <summary>
    /// Gets the command key of this platform, the one that <c>Ctrl</c> stands for in a shortcut: Cmd on macOS, Ctrl
    /// elsewhere.
    /// </summary>
    internal static KeyModifiers CommandModifier => OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Ctrl;

    /// <summary>
    /// Gets the modifier keys actually expected on this platform (<c>Ctrl</c> becomes Cmd on macOS).
    /// </summary>
    internal KeyModifiers Effective => Modifiers.HasFlag(KeyModifiers.Ctrl)
        ? (Modifiers & ~KeyModifiers.Ctrl) | CommandModifier
        : Modifiers;

    /// <summary>
    /// Determines whether a key pressed with modifier keys triggers this shortcut on this platform.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="modifiers">The modifier keys held, as reported by the platform.</param>
    /// <returns><see langword="true"/> if the key and the modifiers match this shortcut; otherwise, <see langword="false"/>.</returns>
    public bool Matches(KeyCode key, KeyModifiers modifiers) => key == Key && modifiers == Effective;

    /// <summary>
    /// Gets a value indicating whether this shortcut uses Ctrl, Alt or Cmd, or a function key, so that it does not
    /// interfere with typing text.
    /// </summary>
    public bool IsCommand =>
        (Effective != KeyModifiers.None && Effective != KeyModifiers.Shift) || Key is >= KeyCode.F1 and <= KeyCode.F12;

    /// <summary>
    /// Returns the text displayed next to a menu item: <c>Ctrl+Shift+S</c>, or <c>⇧⌘S</c> on macOS.
    /// </summary>
    /// <returns>The shortcut, written with the conventions of this platform.</returns>
    public override string ToString()
    {
        string key = Key switch
        {
            >= KeyCode.D0 and <= KeyCode.D9 => ((char)('0' + (Key - KeyCode.D0))).ToString(),
            KeyCode.Plus => "+",
            KeyCode.Minus => "-",
            KeyCode.Equal => "=",
            KeyCode.Comma => ",",
            KeyCode.Period => ".",
            KeyCode.Slash => "/",
            KeyCode.Escape => "Esc",
            KeyCode.Delete => "Del",
            KeyCode.PageUp => "PgUp",
            KeyCode.PageDown => "PgDn",
            _ => Key.ToString(),
        };
        KeyModifiers modifiers = Effective;
        if (OperatingSystem.IsMacOS())
        {
            return (modifiers.HasFlag(KeyModifiers.Ctrl) ? "⌃" : string.Empty) + (modifiers.HasFlag(KeyModifiers.Alt) ? "⌥" : string.Empty)
                 + (modifiers.HasFlag(KeyModifiers.Shift) ? "⇧" : string.Empty) + (modifiers.HasFlag(KeyModifiers.Meta) ? "⌘" : string.Empty) + key;
        }

        return (modifiers.HasFlag(KeyModifiers.Ctrl) ? "Ctrl+" : string.Empty) + (modifiers.HasFlag(KeyModifiers.Shift) ? "Shift+" : string.Empty)
             + (modifiers.HasFlag(KeyModifiers.Alt) ? "Alt+" : string.Empty) + (modifiers.HasFlag(KeyModifiers.Meta) ? "Win+" : string.Empty) + key;
    }
}
