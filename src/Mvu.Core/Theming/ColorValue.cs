using Mvu.Primitives;

namespace Mvu.Theming;

/// <summary>
/// Une couleur fixe, un rôle du thème, ou une couleur du thème désignée par son nom.
/// <code>
/// .Background("#FF0000")          // fixe
/// .Background(ColorRole.Surface)  // rôle : suit le thème actif
/// .Background("success")          // couleur ajoutée au thème par WithColor("success", …)
/// </code>
/// </summary>
public readonly record struct ColorValue
{
    readonly Color _color;
    readonly ColorRole? _role;
    readonly string? _name;

    ColorValue(Color color, ColorRole? role, string? name) { _color = color; _role = role; _name = name; }

    public static implicit operator ColorValue(Color c) => new(c, null, null);
    public static implicit operator ColorValue(ColorRole r) => new(default, r, null);
    /// <summary>"#RRGGBB" / "#RRGGBBAA" = couleur fixe ; tout autre texte = nom d'une couleur du thème.</summary>
    public static implicit operator ColorValue(string value) =>
        value.StartsWith('#') ? new(Color.Hex(value), null, null) : new(default, null, value);

    public Color Resolve(Palette p) => _role is { } r ? p[r] : _name is { } n ? p[n] : _color;
}
