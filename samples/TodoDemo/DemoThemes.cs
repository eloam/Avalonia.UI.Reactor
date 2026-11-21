// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace TodoDemo;

/// <summary>
/// The theme catalog of the sample: the Standard family (the Light and Dark themes of the framework) and the homemade
/// "Sable" family, with its night variant. Each theme gets what the sample uses on top of the standard roles: a
/// "success" color, a "success" button style, and a "caption" text style.
/// </summary>
public static class DemoThemes
{
    private static Theme WithDemoExtras(this Theme theme, Color success) => theme
        .WithColor("success", success)
        .WithButtonStyle("success", new ButtonVariant(Background: "success", Foreground: ColorRole.OnAccent))
        .WithTextStyle("caption", size: 12);

    public static readonly Theme Light = Theme.Light.WithDemoExtras(success: "#16A34A");
    public static readonly Theme Dark = Theme.Dark.WithDemoExtras(success: "#22C55E");

    /// <summary>
    /// The Sable style, shared by its two variants: font, buttons and control templates.
    /// </summary>
    private static Theme WithSableStyle(this Theme theme) => theme
        .WithFont("Georgia", bodySize: 15)
        .WithButton(b => b with { Radius = 999, Padding = new(18, 8), Bold = true })
        // Templates change the structure of the controls, not only their colors:
        // underlined fields instead of framed ones…
        .WithTextField(f => f with
        {
            Template = field => VStack(
                    field.Editor,
                    Divider().Height(2).Background(
                        field.Focused ? ColorRole.Accent : field.Hovered ? ColorRole.TextMuted : ColorRole.FieldBorder))
                .Spacing(4)
                .Padding(2, 6),
        })
        // …and round checkboxes.
        .WithCheckbox(c => c with
        {
            Template = box => HStack(
                Canvas(d =>
                {
                    Point center = d.Bounds.Center;
                    if (box.IsChecked)
                    {
                        d.FillCircle(center, 10, ColorRole.Accent);
                        d.FillCircle(center, 4, ColorRole.OnAccent);
                    }
                    else
                    {
                        d.StrokeCircle(center, 9.25f, box.Hovered ? ColorRole.Accent : ColorRole.FieldBorder, 1.5f);
                    }
                }).Size(20, 20),
                box.Label is { } label ? Text(label) : null),
        });

    /// <summary>
    /// A theme of your own: start from the light theme and change only what differs.
    /// </summary>
    public static readonly Theme Sable = Theme.Light
        .Named("Sable")
        .WithPalette(p => p with
        {
            Page = "#FBF7F2",
            Border = "#EADFD3",
            FieldBorder = "#D9C8B6",
            Text = "#2B1D14",
            TextMuted = "#8A7462",
            Accent = "#C2410C",
            Secondary = "#F5E9DD",
            OnSecondary = "#7C2D12",
        })
        .WithSableStyle()
        .WithDemoExtras(success: "#4D7C0F");

    /// <summary>
    /// Its dark variant: the same style with a night palette, starting from the dark theme.
    /// </summary>
    public static readonly Theme SableNight = Theme.Dark
        .Named("Sable Night")
        .WithPalette(p => p with
        {
            Page = "#1A1512",
            Surface = "#241D18",
            Border = "#3A2F27",
            FieldBorder = "#4D4036",
            Text = "#F3E9DC",
            TextMuted = "#B5A190",
            Accent = "#F97316",
            OnAccent = "#1A1512",
            Secondary = "#3A2F27",
            OnSecondary = "#F3E9DC",
        })
        .WithSableStyle()
        .WithDemoExtras(success: "#84CC16");

    /// <summary>
    /// Creates the catalog of a window. The first family dresses the application by default; views
    /// choose the others by name: <c>.Theme("Sable")</c>.
    /// </summary>
    public static Themes Create() => new()
    {
        { "Standard", Light, Dark },
        { "Sable", Sable, SableNight },
    };
}
