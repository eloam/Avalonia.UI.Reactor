using Mvu.Primitives;
using Mvu.Theming;
using Mvu.Views;

namespace TodoDemo;

/// <summary>
/// Le catalogue de thèmes de la démo : la famille Standard (Clair et Sombre du framework) et la famille
/// « Sable », faite maison, avec sa variante de nuit. Chaque thème reçoit ce que la démo utilise en plus
/// des rôles standard : une couleur « success », un style de bouton « success » et un style de texte « caption ».
/// </summary>
public static class DemoThemes
{
    static Theme WithDemoExtras(this Theme theme, Color success) => theme
        .WithColor("success", success)
        .WithButtonStyle("success", new ButtonVariant(Background: "success", Foreground: ColorRole.OnAccent))
        .WithTextStyle("caption", size: 12);

    public static readonly Theme Light = Theme.Light.WithDemoExtras(success: "#16A34A");
    public static readonly Theme Dark = Theme.Dark.WithDemoExtras(success: "#22C55E");

    /// <summary>Le style « Sable », commun à ses deux variantes : police, boutons et modèles de contrôles.</summary>
    static Theme WithSableStyle(this Theme theme) => theme
        .WithFont("Georgia", bodySize: 15)
        .WithButton(b => b with { Radius = 999, Padding = new(18, 8), Bold = true })
        // Les modèles changent la structure des contrôles, pas seulement leurs couleurs :
        // des champs soulignés au lieu d'encadrés…
        .WithTextField(f => f with
        {
            Template = field => VStack(
                    field.Editor,
                    Divider().Height(2).Background(
                        field.Focused ? ColorRole.Accent : field.Hovered ? ColorRole.TextMuted : ColorRole.FieldBorder))
                .Spacing(4)
                .Padding(2, 6),
        })
        // …et des cases à cocher rondes.
        .WithCheckbox(c => c with
        {
            Template = box => HStack(
                Canvas(d =>
                {
                    var center = d.Bounds.Center;
                    if (box.IsChecked)
                    {
                        d.FillCircle(center, 10, ColorRole.Accent);
                        d.FillCircle(center, 4, ColorRole.OnAccent);
                    }
                    else d.StrokeCircle(center, 9.25f, box.Hovered ? ColorRole.Accent : ColorRole.FieldBorder, 1.5f);
                }).Size(20, 20),
                box.Label is { } label ? Text(label) : null),
        });

    /// <summary>Un thème à soi : on part du thème clair et on ne change que ce qui diffère.</summary>
    public static readonly Theme Sable = Theme.Light
        .Named("Sable")
        .WithPalette(p => p with
        {
            Page = "#FBF7F2", Border = "#EADFD3", FieldBorder = "#D9C8B6",
            Text = "#2B1D14", TextMuted = "#8A7462",
            Accent = "#C2410C", Secondary = "#F5E9DD", OnSecondary = "#7C2D12",
        })
        .WithSableStyle()
        .WithDemoExtras(success: "#4D7C0F");

    /// <summary>Sa variante sombre : même style, palette de nuit (on part du thème sombre).</summary>
    public static readonly Theme SableNight = Theme.Dark
        .Named("Sable nuit")
        .WithPalette(p => p with
        {
            Page = "#1A1512", Surface = "#241D18", Border = "#3A2F27", FieldBorder = "#4D4036",
            Text = "#F3E9DC", TextMuted = "#B5A190",
            Accent = "#F97316", OnAccent = "#1A1512", Secondary = "#3A2F27", OnSecondary = "#F3E9DC",
        })
        .WithSableStyle()
        .WithDemoExtras(success: "#84CC16");

    /// <summary>
    /// Le catalogue d'une fenêtre. La première famille habille l'application par défaut ; les vues
    /// choisissent les autres par leur nom : <c>.Theme("Sable")</c>.
    /// </summary>
    public static Themes Create() => new()
    {
        { "Standard", Light, Dark },
        { "Sable", Sable, SableNight },
    };
}
