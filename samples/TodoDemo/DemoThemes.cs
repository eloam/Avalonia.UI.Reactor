// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace TodoDemo;

/// <summary>
/// Provides the theme catalog of the sample: the Standard family (the Light and Dark themes of the framework) and the
/// homemade Sable family, with its night variant.
/// </summary>
/// <remarks>
/// Each theme gets what the sample uses on top of the standard roles: a <see cref="Success"/> color and button style,
/// and a <see cref="Caption"/> text style.
/// </remarks>
public static class DemoThemes
{
    /// <summary>
    /// The name of the family made of the Light and Dark themes of the framework.
    /// </summary>
    public const string StandardFamily = "Standard";

    /// <summary>
    /// The name of the homemade family.
    /// </summary>
    public const string SableFamily = "Sable";

    /// <summary>
    /// The name of the color and of the button style that the sample adds to each theme.
    /// </summary>
    public const string Success = "success";

    /// <summary>
    /// The name of the text style that the sample adds to each theme.
    /// </summary>
    public const string Caption = "caption";

    /// <summary>
    /// The light theme of the framework, with the additions of the sample.
    /// </summary>
    public static readonly Theme Light = Theme.Light.WithDemoExtras(success: "#16A34A");

    /// <summary>
    /// The dark theme of the framework, with the additions of the sample.
    /// </summary>
    public static readonly Theme Dark = Theme.Dark.WithDemoExtras(success: "#22C55E");

    /// <summary>
    /// A theme of your own: start from the light theme and change only what differs.
    /// </summary>
    public static readonly Theme Sable = Theme.Light
        .Named("Sable")
        .WithPalette(palette => palette with
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
        .WithPalette(palette => palette with
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
    /// Creates the catalog of a window. The first family dresses the application by default; views choose the others
    /// by name: <c>.Theme(DemoThemes.SableFamily)</c>.
    /// </summary>
    /// <returns>The catalog of the Standard and Sable families.</returns>
    public static Themes Create() => new()
    {
        { StandardFamily, Light, Dark },
        { SableFamily, Sable, SableNight },
    };

    private static Theme WithDemoExtras(this Theme theme, Color success) => theme
        .WithColor(Success, success)
        .WithButtonStyle(Success, new ButtonVariant(Background: Success, Foreground: ColorRole.OnAccent))
        .WithTextStyle(Caption, size: 12);

    // The Sable style, shared by its two variants: font, buttons and control templates.
    private static Theme WithSableStyle(this Theme theme) => theme
        .WithFont("Georgia", bodySize: 15)
        .WithButton(button => button with { Radius = 999, Padding = new(18, 8), Bold = true })
        // Templates change the structure of the controls, not only their colors: underlined fields instead of framed
        // ones…
        .WithTextField(field => field with
        {
            Template = state => VStack(
                    state.Editor,
                    Divider().Height(2).Background(
                        state.Focused ? ColorRole.Accent : state.Hovered ? ColorRole.TextMuted : ColorRole.FieldBorder))
                .Spacing(4)
                .Padding(2, 6),
        })
        // …and round checkboxes.
        .WithCheckbox(checkbox => checkbox with
        {
            Template = state => HStack(
                Canvas(drawing =>
                {
                    Point center = drawing.Bounds.Center;
                    if (state.IsChecked)
                    {
                        drawing.FillCircle(center, 10, ColorRole.Accent);
                        drawing.FillCircle(center, 4, ColorRole.OnAccent);
                    }
                    else
                    {
                        drawing.StrokeCircle(center, 9.25f, state.Hovered ? ColorRole.Accent : ColorRole.FieldBorder, 1.5f);
                    }
                }).Size(20, 20),
                state.Label is { } label ? Text(label) : null),
        });
}
