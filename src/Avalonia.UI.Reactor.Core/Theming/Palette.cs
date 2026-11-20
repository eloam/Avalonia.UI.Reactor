using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Les couleurs d'un thème : des rôles connus (<see cref="ColorRole"/>) et des couleurs à soi,
/// ajoutées par nom (<c>palette.With("success", "#15803D")</c>).
/// </summary>
public sealed record Palette(
    Color Page, Color Surface, Color Border, Color FieldBorder,
    Color Text, Color TextMuted,
    Color Accent, Color OnAccent,
    Color Secondary, Color OnSecondary,
    Color Danger, Color OnDanger)
{
    /// <summary>Couleur affichée pour un nom inconnu du thème : bien visible, pour repérer l'oubli.</summary>
    public static readonly Color Missing = new(255, 0, 255);

    /// <summary>Couleurs ajoutées par nom (insensible à la casse).</summary>
    public IReadOnlyDictionary<string, Color> Custom { get; init; } =
        new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

    public Color this[ColorRole role] => role switch
    {
        ColorRole.Page => Page,
        ColorRole.Surface => Surface,
        ColorRole.Border => Border,
        ColorRole.FieldBorder => FieldBorder,
        ColorRole.Text => Text,
        ColorRole.TextMuted => TextMuted,
        ColorRole.Accent => Accent,
        ColorRole.OnAccent => OnAccent,
        ColorRole.Secondary => Secondary,
        ColorRole.OnSecondary => OnSecondary,
        ColorRole.Danger => Danger,
        ColorRole.OnDanger => OnDanger,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    /// <summary>Par nom : un rôle connu ("accent", "page"…) ou une couleur ajoutée ; sinon <see cref="Missing"/>.</summary>
    public Color this[string name] =>
        Enum.TryParse<ColorRole>(name, ignoreCase: true, out var role) ? this[role]
        : Custom.TryGetValue(name, out var c) ? c
        : Missing;

    public Palette With(ColorRole role, Color color) => role switch
    {
        ColorRole.Page => this with { Page = color },
        ColorRole.Surface => this with { Surface = color },
        ColorRole.Border => this with { Border = color },
        ColorRole.FieldBorder => this with { FieldBorder = color },
        ColorRole.Text => this with { Text = color },
        ColorRole.TextMuted => this with { TextMuted = color },
        ColorRole.Accent => this with { Accent = color },
        ColorRole.OnAccent => this with { OnAccent = color },
        ColorRole.Secondary => this with { Secondary = color },
        ColorRole.OnSecondary => this with { OnSecondary = color },
        ColorRole.Danger => this with { Danger = color },
        ColorRole.OnDanger => this with { OnDanger = color },
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public Palette With(string name, Color color)
    {
        if (Enum.TryParse<ColorRole>(name, ignoreCase: true, out var role)) return With(role, color);
        return this with
        {
            Custom = new Dictionary<string, Color>(Custom, StringComparer.OrdinalIgnoreCase) { [name] = color },
        };
    }

    public static readonly Palette Light = new(
        Page: "#F3F4F6", Surface: "#FFFFFF", Border: "#E5E7EB", FieldBorder: "#D1D5DB",
        Text: "#111827", TextMuted: "#6B7280",
        Accent: "#2563EB", OnAccent: "#FFFFFF",
        Secondary: "#E5E7EB", OnSecondary: "#111827",
        Danger: "#DC2626", OnDanger: "#FFFFFF");

    public static readonly Palette Dark = new(
        Page: "#0B0F17", Surface: "#161B26", Border: "#262D3B", FieldBorder: "#384152",
        Text: "#F3F4F6", TextMuted: "#9CA3AF",
        Accent: "#3B82F6", OnAccent: "#FFFFFF",
        Secondary: "#262D3B", OnSecondary: "#F3F4F6",
        Danger: "#EF4444", OnDanger: "#FFFFFF");
}
