using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Environnement hérité par une branche de vues, comme celui de SwiftUI : catalogue, famille et mode en cours,
/// thème qui en découle, police et couleur du texte. Tous les moteurs de rendu le résolvent de la même façon :
/// <see cref="Enter"/> applique les thèmes déclarés par une vue, <see cref="FontFor"/> et <see cref="ForegroundFor"/>
/// ses styles de texte.
/// </summary>
internal readonly record struct ViewEnvironment(
    Themes Themes, ThemeFamily Family, ColorScheme Scheme, Theme Theme, Font Font, Color Foreground)
{
    /// <summary>Départ : première famille du catalogue, mode du système.</summary>
    public static ViewEnvironment Root(Themes themes)
    {
        var family = themes.DefaultFamily;
        var scheme = themes.SystemScheme;
        var theme = family.For(scheme);
        return new(themes, family, scheme, theme, BaseFont(theme), theme.Palette.Text);
    }

    /// <summary>Environnement d'une vue placée sous un contrôle (la vue de son modèle) : thème, police et couleur du contrôle.</summary>
    public static ViewEnvironment Under(Themes themes, Theme theme, Font font, Color foreground) => new(
        themes, themes.FamilyOf(theme) ?? ThemeFamily.Of(theme.Name, theme), theme.Scheme, theme, font, foreground);

    /// <summary>
    /// Thème d'une branche, déclaré par la vue elle-même :
    /// <c>.Theme("Sable")</c> (famille du catalogue), <c>.Theme(famille)</c>, <c>.Theme(thème)</c> (thème précis)
    /// et/ou <c>.ColorScheme(…)</c> (variante claire ou sombre). Sans rien de tout cela, la branche hérite du parent.
    /// Quand le thème change, police et couleur du texte repartent de ses valeurs de base.
    /// </summary>
    public ViewEnvironment Enter(View v)
    {
        if (v.Theme is null && v.ThemeName is null && v.ThemeFamily is null && v.Scheme is null) return this;

        var family = Family;
        var exact = v.Theme;
        if (v.ThemeFamily is { } f) family = f;
        else if (v.ThemeName is { } name)
        {
            if (Themes.FamilyNamed(name) is { } named) family = named;
            else if (Themes[name] is { } byName) exact ??= byName;
            else Themes.WarnUnknown(name);
        }

        // Un thème précis apporte sa famille (pour les .ColorScheme plus bas) et son mode.
        if (exact is not null) family = Themes.FamilyOf(exact) ?? ThemeFamily.Of(exact.Name, exact);
        var scheme = v.Scheme ?? exact?.Scheme ?? Scheme;
        var theme = exact is not null && v.Scheme is null ? exact : family.For(scheme);

        var env = this;
        if (!ReferenceEquals(theme, Theme))
            env = env with { Theme = theme, Font = BaseFont(theme), Foreground = theme.Palette.Text };
        return env with { Family = family, Scheme = scheme };
    }

    /// <summary>Police d'une vue : modificateur explicite, sinon style du thème, sinon défaut du contrôle, sinon héritée.</summary>
    public Font FontFor(View v)
    {
        var typo = Theme.Typography;
        var spec = v.TextStyleName is { } styleName ? typo[styleName]
                   : v.TextStyle is { } style ? typo[style]
                   : null;
        float size = v.FontSize
                     ?? spec?.Size
                     ?? (v is ButtonView ? Theme.Button.FontSize : null)
                     ?? Font.Size;
        int weight = v.Bold is { } bold ? (bold ? typo.BoldWeight : typo.Body.Weight)
                     : spec?.Weight
                       ?? (v is ButtonView && Theme.Button.Bold ? typo.BoldWeight : (int?)null)
                       ?? Font.Weight;
        return new Font(typo.Family, size, weight);
    }

    /// <summary>Couleur du texte d'une vue : la sienne, sinon celle héritée.</summary>
    public Color ForegroundFor(View v) => v.Foreground?.Resolve(Theme.Palette) ?? Foreground;

    static Font BaseFont(Theme t) => new(t.Typography.Family, t.Typography.Body.Size, t.Typography.Body.Weight);
}
