namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Catalogue des tests : la famille Standard du framework et une famille « Sable » faite maison, comme celle de la démo
/// (palette, police, boutons en pilule), en clair et en nuit.
/// </summary>
internal static class TestThemes
{
    public static readonly Theme Sable = Theme.Light
        .Named("Sable")
        .WithPalette(p => p with
        {
            Page = "#FBF7F2", Border = "#EADFD3", FieldBorder = "#D9C8B6", Text = "#2B1D14", TextMuted = "#8A7462",
            Accent = "#C2410C", Secondary = "#F5E9DD", OnSecondary = "#7C2D12",
        })
        .WithFont("Georgia", bodySize: 15)
        .WithButton(b => b with { Radius = 999, Padding = new(18, 8), Bold = true });

    public static readonly Theme SableNight = Theme.Dark
        .Named("Sable nuit")
        .WithPalette(p => p with
        {
            Page = "#1A1512", Surface = "#241D18", Border = "#3A2F27", FieldBorder = "#4D4036", Text = "#F3E9DC",
            TextMuted = "#B5A190", Accent = "#F97316", OnAccent = "#1A1512", Secondary = "#3A2F27", OnSecondary = "#F3E9DC",
        })
        .WithFont("Georgia", bodySize: 15)
        .WithButton(b => b with { Radius = 999, Padding = new(18, 8), Bold = true });

    public static Themes Create() => new()
    {
        { "Standard", Theme.Light, Theme.Dark },
        { "Sable", Sable, SableNight },
    };

    /// <summary>Le thème d'une famille du catalogue, dans un mode.</summary>
    public static Theme Of(string family, ColorScheme scheme) => Create().FamilyNamed(family)!.For(scheme);
}
