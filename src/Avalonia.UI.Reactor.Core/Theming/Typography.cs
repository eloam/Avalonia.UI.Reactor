// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents the font and the text styles of a theme: the five built-in styles (<see cref="TextStyle"/>) and styles
/// added by name.
/// </summary>
/// <remarks>
/// A style added with <c>theme.WithTextStyle("caption", size: 11)</c> is used with <c>.Font("caption")</c>.
/// </remarks>
public sealed record Typography
{
    /// <summary>
    /// Gets the name of the system font of this platform: Segoe UI, Helvetica Neue, Roboto or the generic sans-serif.
    /// </summary>
    public static string SystemFamily =>
        OperatingSystem.IsWindows() ? "Segoe UI"
        : OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() ? "Helvetica Neue"
        : OperatingSystem.IsAndroid() ? "Roboto"
        : "sans-serif";

    /// <summary>
    /// Gets the font family; by default, the system font.
    /// </summary>
    public string Family { get; init; } = SystemFamily;

    /// <summary>
    /// Gets the size and weight of the <see cref="TextStyle.Small"/> style.
    /// </summary>
    public TextStyleSpec Small { get; init; } = new(13);

    /// <summary>
    /// Gets the size and weight of the <see cref="TextStyle.Body"/> style.
    /// </summary>
    public TextStyleSpec Body { get; init; } = new(14);

    /// <summary>
    /// Gets the size and weight of the <see cref="TextStyle.Headline"/> style.
    /// </summary>
    public TextStyleSpec Headline { get; init; } = new(15, FontWeight.SemiBold);

    /// <summary>
    /// Gets the size and weight of the <see cref="TextStyle.Title"/> style.
    /// </summary>
    public TextStyleSpec Title { get; init; } = new(24, FontWeight.SemiBold);

    /// <summary>
    /// Gets the size and weight of the <see cref="TextStyle.LargeTitle"/> style.
    /// </summary>
    public TextStyleSpec LargeTitle { get; init; } = new(32, FontWeight.SemiBold);

    /// <summary>
    /// Gets the weight applied by the <c>.Bold()</c> modifier.
    /// </summary>
    public int BoldWeight { get; init; } = FontWeight.SemiBold;

    /// <summary>
    /// Gets the styles added by name; names are case-insensitive.
    /// </summary>
    public IReadOnlyDictionary<string, TextStyleSpec> Custom { get; init; } =
        new Dictionary<string, TextStyleSpec>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the size and weight of a built-in style.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <returns>The size and weight of the style.</returns>
    public TextStyleSpec this[TextStyle style] => style switch
    {
        TextStyle.Small => Small,
        TextStyle.Headline => Headline,
        TextStyle.Title => Title,
        TextStyle.LargeTitle => LargeTitle,
        _ => Body,
    };

    /// <summary>
    /// Gets a style by name: a built-in style (<c>"title"</c>…) or an added one.
    /// </summary>
    /// <param name="name">The name of the style, case-insensitive.</param>
    /// <returns>The style, or <see langword="null"/> if the name is unknown: the text keeps its inherited style.</returns>
    public TextStyleSpec? this[string name] =>
        Enum.TryParse(name, ignoreCase: true, out TextStyle style) ? this[style]
        : Custom.TryGetValue(name, out TextStyleSpec? spec) ? spec
        : null;

    /// <summary>
    /// Returns a copy of this typography where a style has a new size and weight.
    /// </summary>
    /// <param name="name">The name of a built-in style, or of a style to add or replace.</param>
    /// <param name="spec">The size and weight of the style.</param>
    /// <returns>The updated typography.</returns>
    public Typography With(string name, TextStyleSpec spec)
    {
        if (!Enum.TryParse(name, ignoreCase: true, out TextStyle style))
        {
            return this with
            {
                Custom = new Dictionary<string, TextStyleSpec>(Custom, StringComparer.OrdinalIgnoreCase) { [name] = spec },
            };
        }

        return style switch
        {
            TextStyle.Small => this with { Small = spec },
            TextStyle.Headline => this with { Headline = spec },
            TextStyle.Title => this with { Title = spec },
            TextStyle.LargeTitle => this with { LargeTitle = spec },
            _ => this with { Body = spec },
        };
    }
}
