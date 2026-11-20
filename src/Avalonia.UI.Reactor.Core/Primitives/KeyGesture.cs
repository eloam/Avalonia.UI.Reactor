namespace Avalonia.UI.Reactor.Primitives;

/// <summary>
/// Raccourci clavier : une touche et ses modificateurs. S'écrit comme on le lit :
/// <c>KeyGesture.Parse("Ctrl+Shift+S")</c>, <c>"F5"</c>, <c>"Ctrl+="</c>.
/// Sur macOS, <c>Ctrl</c> désigne la touche Cmd, comme le veulent les conventions du système.
/// </summary>
public readonly record struct KeyGesture(KeyCode Key, KeyModifiers Modifiers = KeyModifiers.None)
{
    static readonly Dictionary<string, KeyCode> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = KeyCode.Enter, ["Return"] = KeyCode.Enter,
        ["Esc"] = KeyCode.Escape, ["Escape"] = KeyCode.Escape,
        ["Tab"] = KeyCode.Tab, ["Space"] = KeyCode.Space, ["Backspace"] = KeyCode.Backspace,
        ["Del"] = KeyCode.Delete, ["Delete"] = KeyCode.Delete, ["Ins"] = KeyCode.Insert, ["Insert"] = KeyCode.Insert,
        ["Home"] = KeyCode.Home, ["End"] = KeyCode.End,
        ["PgUp"] = KeyCode.PageUp, ["PageUp"] = KeyCode.PageUp, ["PgDn"] = KeyCode.PageDown, ["PageDown"] = KeyCode.PageDown,
        ["Up"] = KeyCode.Up, ["Down"] = KeyCode.Down, ["Left"] = KeyCode.Left, ["Right"] = KeyCode.Right,
        ["Plus"] = KeyCode.Plus, ["+"] = KeyCode.Plus, ["Minus"] = KeyCode.Minus, ["-"] = KeyCode.Minus,
        ["="] = KeyCode.Equal, [","] = KeyCode.Comma, ["."] = KeyCode.Period, ["/"] = KeyCode.Slash,
    };

    /// <summary>Lit un raccourci écrit comme <c>"Ctrl+N"</c>, <c>"Ctrl+Shift+S"</c>, <c>"Alt+F4"</c> ou <c>"F5"</c>.</summary>
    public static KeyGesture Parse(string text)
    {
        // "Ctrl++" : le dernier « + » est la touche, pas un séparateur.
        var parts = text.EndsWith("++", StringComparison.Ordinal)
            ? [.. text[..^2].Split('+', StringSplitOptions.TrimEntries), "+"]
            : text.Split('+', StringSplitOptions.TrimEntries);

        var modifiers = KeyModifiers.None;
        foreach (var part in parts[..^1])
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

    static KeyCode? ParseKey(string key)
    {
        if (key.Length == 1 && char.IsAsciiLetter(key[0])) return KeyCode.A + (char.ToUpperInvariant(key[0]) - 'A');
        if (key.Length == 1 && char.IsAsciiDigit(key[0])) return KeyCode.D0 + (key[0] - '0');
        if (key.Length is 2 or 3 && (key[0] is 'F' or 'f') && int.TryParse(key[1..], out int n) && n is >= 1 and <= 12)
            return KeyCode.F1 + (n - 1);
        return Names.TryGetValue(key, out var code) ? code : null;
    }

    static ArgumentException Invalid(string text) =>
        new($"'{text}' is not a valid keyboard shortcut. Use a form such as 'Ctrl+N', 'Ctrl+Shift+S' or 'F5'.", nameof(text));

    /// <summary>Touche des commandes de cette plateforme, celle que désigne <c>Ctrl</c> dans un raccourci : Cmd sur macOS, Ctrl ailleurs.</summary>
    internal static KeyModifiers CommandModifier => OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Ctrl;

    /// <summary>Modificateurs réellement attendus sur cette plateforme (Ctrl devient Cmd sur macOS).</summary>
    KeyModifiers Effective => Modifiers.HasFlag(KeyModifiers.Ctrl)
        ? (Modifiers & ~KeyModifiers.Ctrl) | CommandModifier
        : Modifiers;

    public bool Matches(KeyCode key, KeyModifiers modifiers) => key == Key && modifiers == Effective;

    /// <summary>Raccourci avec Ctrl, Alt ou Cmd, ou touche de fonction : il ne gêne pas la saisie de texte.</summary>
    public bool IsCommand => Effective != KeyModifiers.None && Effective != KeyModifiers.Shift || Key is >= KeyCode.F1 and <= KeyCode.F12;

    /// <summary>Texte affiché à côté d'un élément de menu : <c>Ctrl+Shift+S</c>, ou <c>⇧⌘S</c> sur macOS.</summary>
    public override string ToString()
    {
        string key = Key switch
        {
            >= KeyCode.D0 and <= KeyCode.D9 => ((char)('0' + (Key - KeyCode.D0))).ToString(),
            KeyCode.Plus => "+", KeyCode.Minus => "-", KeyCode.Equal => "=",
            KeyCode.Comma => ",", KeyCode.Period => ".", KeyCode.Slash => "/",
            KeyCode.Escape => "Esc", KeyCode.Delete => "Del", KeyCode.PageUp => "PgUp", KeyCode.PageDown => "PgDn",
            _ => Key.ToString(),
        };
        var m = Effective;
        if (OperatingSystem.IsMacOS())
            return (m.HasFlag(KeyModifiers.Ctrl) ? "⌃" : "") + (m.HasFlag(KeyModifiers.Alt) ? "⌥" : "")
                 + (m.HasFlag(KeyModifiers.Shift) ? "⇧" : "") + (m.HasFlag(KeyModifiers.Meta) ? "⌘" : "") + key;
        return (m.HasFlag(KeyModifiers.Ctrl) ? "Ctrl+" : "") + (m.HasFlag(KeyModifiers.Shift) ? "Shift+" : "")
             + (m.HasFlag(KeyModifiers.Alt) ? "Alt+" : "") + (m.HasFlag(KeyModifiers.Meta) ? "Win+" : "") + key;
    }
}
