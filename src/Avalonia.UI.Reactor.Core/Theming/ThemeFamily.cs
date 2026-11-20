namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Une famille de thèmes : une variante claire et/ou une variante sombre réunies sous un nom.
/// Avec ses deux variantes, la famille suit le mode du système (ou celui imposé par <c>.ColorScheme(…)</c>) ;
/// avec une seule, c'est toujours celle-là.
/// </summary>
public sealed record ThemeFamily
{
    public ThemeFamily(string name, Theme? light, Theme? dark)
    {
        _fallback = light ?? dark
                    ?? throw new ArgumentException($"Theme family '{name}' must contain at least one theme.");
        if (light is { Scheme: not ColorScheme.Light })
            throw new ArgumentException($"Theme '{light.Name}' is a dark theme and cannot be the light variant of theme family '{name}'.");
        if (dark is { Scheme: not ColorScheme.Dark })
            throw new ArgumentException($"Theme '{dark.Name}' is a light theme and cannot be the dark variant of theme family '{name}'.");
        Name = name;
        Light = light;
        Dark = dark;
    }

    /// <summary>Clair et Sombre, fournis par le framework.</summary>
    public static readonly ThemeFamily Standard = new("Standard", Theme.Light, Theme.Dark);

    /// <summary>La variante qui existe, quand l'autre manque (la famille en a toujours au moins une).</summary>
    readonly Theme _fallback;

    public string Name { get; }
    public Theme? Light { get; }
    public Theme? Dark { get; }

    /// <summary>La famille a une variante claire et une sombre : elle suit le mode du système.</summary>
    public bool FollowsSystem => Light is not null && Dark is not null;

    /// <summary>Les thèmes de la famille (un ou deux).</summary>
    public IEnumerable<Theme> Variants => new[] { Light, Dark }.OfType<Theme>();

    /// <summary>Variante pour ce mode ; à défaut, celle qui existe.</summary>
    public Theme For(ColorScheme scheme) => (scheme == ColorScheme.Dark ? Dark : Light) ?? _fallback;

    public bool Contains(Theme theme) => Equals(Light, theme) || Equals(Dark, theme);

    /// <summary>Famille d'un ou deux thèmes, rangés selon leur mode : l'ordre n'a pas d'importance.</summary>
    public static ThemeFamily Of(string name, Theme theme, Theme? other = null)
    {
        if (other is not null && other.Scheme == theme.Scheme)
            throw new ArgumentException(
                $"Theme family '{name}' cannot contain two {(theme.Scheme == ColorScheme.Dark ? "dark" : "light")} themes; it takes one light theme and one dark theme.");
        return new ThemeFamily(name,
            light: theme.Scheme == ColorScheme.Light ? theme : other,
            dark: theme.Scheme == ColorScheme.Dark ? theme : other);
    }
}
