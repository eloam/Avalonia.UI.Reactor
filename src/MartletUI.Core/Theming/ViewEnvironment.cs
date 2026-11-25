// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;
using MartletUI.Views;

namespace MartletUI.Theming;

/// <summary>
/// Represents the environment inherited by a branch of views, like the environment of SwiftUI: the catalog, the current
/// family and mode, the resulting theme, and the font and text color.
/// </summary>
/// <remarks>
/// <see cref="Enter"/> applies the themes declared by a view; <see cref="FontFor"/> and <see cref="ForegroundFor"/>
/// apply its text styles.
/// </remarks>
/// <param name="Themes">The catalog of the application.</param>
/// <param name="Family">The current family.</param>
/// <param name="Scheme">The current color scheme.</param>
/// <param name="Theme">The resulting theme.</param>
/// <param name="Font">The inherited font.</param>
/// <param name="Foreground">The inherited text color.</param>
internal readonly record struct ViewEnvironment(
    Themes Themes,
    ThemeFamily Family,
    ColorScheme Scheme,
    Theme Theme,
    Font Font,
    Color Foreground)
{
    /// <summary>
    /// Returns the environment at the root of a window: the first family of the catalog, in the system mode.
    /// </summary>
    /// <param name="themes">The catalog of the application.</param>
    /// <returns>The root environment.</returns>
    public static ViewEnvironment Root(Themes themes)
    {
        ThemeFamily family = themes.DefaultFamily;
        ColorScheme scheme = themes.SystemScheme;
        Theme theme = family.For(scheme);
        return new(themes, family, scheme, theme, BaseFont(theme), theme.Palette.Text);
    }

    /// <summary>
    /// Returns the environment of a view, with the theme it declares.
    /// </summary>
    /// <remarks>
    /// A view declares its theme with <c>.Theme("Sable")</c> (a family of the catalog), <c>.Theme(family)</c>,
    /// <c>.Theme(theme)</c> (an exact theme) and/or <c>.ColorScheme(…)</c> (the light or dark variant). Without any of
    /// these, the branch inherits from its parent. When the theme changes, the font and the text color start again from
    /// the base values of the new theme.
    /// </remarks>
    /// <param name="view">The view that may declare a theme.</param>
    /// <returns>The environment of the view.</returns>
    public ViewEnvironment Enter(View view)
    {
        if (view.Theme is null && view.ThemeName is null && view.ThemeFamily is null && view.Scheme is null)
        {
            return this;
        }

        ThemeFamily family = Family;
        Theme? exact = view.Theme;
        if (view.ThemeFamily is { } declaredFamily)
        {
            family = declaredFamily;
        }
        else if (view.ThemeName is { } name)
        {
            if (Themes.FamilyNamed(name) is { } named)
            {
                family = named;
            }
            else if (Themes[name] is { } byName)
            {
                exact ??= byName;
            }
            else
            {
                Themes.WarnUnknown(name);
            }
        }

        // An exact theme brings its family (for the .ColorScheme modifiers below it) and its mode.
        if (exact is not null)
        {
            family = Themes.FamilyOf(exact) ?? ThemeFamily.Of(exact.Name, exact);
        }

        ColorScheme scheme = view.Scheme ?? exact?.Scheme ?? Scheme;
        Theme theme = exact is not null && view.Scheme is null ? exact : family.For(scheme);

        ViewEnvironment environment = this;
        if (!ReferenceEquals(theme, Theme))
        {
            environment = environment with { Theme = theme, Font = BaseFont(theme), Foreground = theme.Palette.Text };
        }

        return environment with { Family = family, Scheme = scheme };
    }

    /// <summary>
    /// Returns the font of a view: its explicit modifiers, otherwise the style of the theme, otherwise the default of
    /// the control, otherwise the inherited font.
    /// </summary>
    /// <param name="view">The view.</param>
    /// <returns>The resolved font.</returns>
    public Font FontFor(View view)
    {
        Typography typography = Theme.Typography;
        TextStyleSpec? spec = view.TextStyleName is { } styleName ? typography[styleName]
            : view.TextStyle is { } style ? typography[style]
            : null;
        float size = view.FontSize
            ?? spec?.Size
            ?? (view is ButtonView ? Theme.Button.FontSize : null)
            ?? Font.Size;
        int weight = view.Bold is { } bold ? (bold ? typography.BoldWeight : typography.Body.Weight)
            : spec?.Weight
                ?? (view is ButtonView && Theme.Button.Bold ? typography.BoldWeight : (int?)null)
                ?? Font.Weight;
        return new Font(typography.Family, size, weight);
    }

    /// <summary>
    /// Returns the text color of a view: its own, otherwise the inherited one.
    /// </summary>
    /// <param name="view">The view.</param>
    /// <returns>The resolved text color.</returns>
    public Color ForegroundFor(View view) => view.Foreground?.Resolve(Theme.Palette) ?? Foreground;

    private static Font BaseFont(Theme theme) =>
        new(theme.Typography.Family, theme.Typography.Body.Size, theme.Typography.Body.Weight);
}
