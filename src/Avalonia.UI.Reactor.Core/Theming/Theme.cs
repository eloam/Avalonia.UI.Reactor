using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Un thème complet, identifié par son nom. Tout est un record immuable : on part d'un thème
/// existant et on ne change que ce qui diffère.
/// <code>
/// var sable = Theme.Light.Named("Sable")
///     .WithColor(ColorRole.Page, "#F6EFE6")
///     .WithColor(ColorRole.Accent, "#B45309")
///     .WithColor("success", "#15803D")          // couleur à soi, utilisable par .Background("success")
///     .WithFont("Georgia")
///     .WithRadius(12);
/// </code>
/// </summary>
public sealed record Theme(
    string Name, ColorScheme Scheme, Palette Palette, Typography Typography,
    ButtonTheme Button, TextFieldTheme TextField, CheckboxTheme Checkbox)
{
    /// <summary>Espacement par défaut entre les enfants de <c>VStack</c> / <c>HStack</c>.</summary>
    public float Spacing { get; init; } = 8;
    public ColorValue DividerColor { get; init; } = ColorRole.Border;
    public float DividerThickness { get; init; } = 1;
    /// <summary>Voile au survol / à l'appui d'une vue cliquable (<c>.OnTap</c>).</summary>
    public float HoverOverlay { get; init; } = 0.04f;
    public float PressedOverlay { get; init; } = 0.08f;
    /// <summary>Barre de menus dessinée et menus ouverts.</summary>
    public MenuTheme Menu { get; init; } = MenuTheme.Default;

    /// <summary>Crée un thème dont tous les styles de contrôles découlent de la palette.</summary>
    public static Theme From(string name, ColorScheme scheme, Palette palette, Typography? typography = null) =>
        new(name, scheme, palette, typography ?? new Typography(),
            ButtonTheme.Default, TextFieldTheme.Default, CheckboxTheme.Default);

    public static readonly Theme Light = From("Clair", ColorScheme.Light, Palette.Light);
    public static readonly Theme Dark = From("Sombre", ColorScheme.Dark, Palette.Dark);

    public Color this[ColorValue c] => c.Resolve(Palette);

    // ── Personnalisation chaînable ──

    /// <summary>Copie sous un autre nom : point de départ d'un thème à soi.</summary>
    public Theme Named(string name) => this with { Name = name };

    public Theme WithPalette(Func<Palette, Palette> f) => this with { Palette = f(Palette) };
    public Theme WithTypography(Func<Typography, Typography> f) => this with { Typography = f(Typography) };
    public Theme WithButton(Func<ButtonTheme, ButtonTheme> f) => this with { Button = f(Button) };
    public Theme WithTextField(Func<TextFieldTheme, TextFieldTheme> f) => this with { TextField = f(TextField) };
    public Theme WithCheckbox(Func<CheckboxTheme, CheckboxTheme> f) => this with { Checkbox = f(Checkbox) };
    public Theme WithMenu(Func<MenuTheme, MenuTheme> f) => this with { Menu = f(Menu) };

    /// <summary>Change une couleur du thème…</summary>
    public Theme WithColor(ColorRole role, Color color) => WithPalette(p => p.With(role, color));
    /// <summary>…ou en ajoute une, référencée ensuite par son nom : <c>.Foreground("success")</c>.</summary>
    public Theme WithColor(string name, Color color) => WithPalette(p => p.With(name, color));

    public Theme WithAccent(Color accent, Color? onAccent = null) =>
        WithPalette(p => p with { Accent = accent, OnAccent = onAccent ?? p.OnAccent });

    public Theme WithFont(string family, float? bodySize = null) =>
        WithTypography(t => t with { Family = family, Body = bodySize is { } s ? t.Body with { Size = s } : t.Body });

    /// <summary>Ajoute (ou remplace) un style de texte, utilisé par <c>.Font("caption")</c>.</summary>
    public Theme WithTextStyle(string name, float size, int weight = FontWeight.Regular) =>
        WithTypography(t => t.With(name, new TextStyleSpec(size, weight)));

    /// <summary>Ajoute (ou remplace) un style de bouton, utilisé par <c>.Style("success")</c>.</summary>
    public Theme WithButtonStyle(string name, ButtonVariant variant) => WithButton(b => b.With(name, variant));

    /// <summary>Même rayon d'angle pour boutons, champs et cases à cocher.</summary>
    public Theme WithRadius(float radius) => this with
    {
        Button = Button with { Radius = radius },
        TextField = TextField with { Radius = radius },
        Checkbox = Checkbox with { Radius = Math.Min(radius, Checkbox.Size / 2) },
    };

    public Theme WithSpacing(float spacing) => this with { Spacing = spacing };
}
