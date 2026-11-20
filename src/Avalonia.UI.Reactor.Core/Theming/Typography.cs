using Avalonia.UI.Reactor.Primitives;
namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Police et styles de texte. Les cinq styles connus (<see cref="TextStyle"/>) et des styles à soi,
/// ajoutés par nom (<c>typography.With("caption", new(11, 500))</c>, utilisés par <c>.Font("caption")</c>).
/// </summary>
public sealed record Typography
{
    /// <summary>Police par défaut : celle du système (Segoe UI, SF/Helvetica, Roboto…).</summary>
    public string Family { get; init; } = SystemFamily;

    public TextStyleSpec Small { get; init; } = new(13);
    public TextStyleSpec Body { get; init; } = new(14);
    public TextStyleSpec Headline { get; init; } = new(15, FontWeight.SemiBold);
    public TextStyleSpec Title { get; init; } = new(24, FontWeight.SemiBold);
    public TextStyleSpec LargeTitle { get; init; } = new(32, FontWeight.SemiBold);

    /// <summary>Graisse appliquée par <c>.Bold()</c>.</summary>
    public int BoldWeight { get; init; } = FontWeight.SemiBold;

    /// <summary>Styles ajoutés par nom (insensible à la casse).</summary>
    public IReadOnlyDictionary<string, TextStyleSpec> Custom { get; init; } =
        new Dictionary<string, TextStyleSpec>(StringComparer.OrdinalIgnoreCase);

    public TextStyleSpec this[TextStyle style] => style switch
    {
        TextStyle.Small => Small,
        TextStyle.Headline => Headline,
        TextStyle.Title => Title,
        TextStyle.LargeTitle => LargeTitle,
        _ => Body,
    };

    /// <summary>Par nom : un style connu ("title"…) ou ajouté ; null si inconnu (le texte garde le style hérité).</summary>
    public TextStyleSpec? this[string name] =>
        Enum.TryParse<TextStyle>(name, ignoreCase: true, out var s) ? this[s]
        : Custom.TryGetValue(name, out var spec) ? spec
        : null;

    public Typography With(string name, TextStyleSpec spec) =>
        Enum.TryParse<TextStyle>(name, ignoreCase: true, out var s)
            ? s switch
            {
                TextStyle.Small => this with { Small = spec },
                TextStyle.Headline => this with { Headline = spec },
                TextStyle.Title => this with { Title = spec },
                TextStyle.LargeTitle => this with { LargeTitle = spec },
                _ => this with { Body = spec },
            }
            : this with { Custom = new Dictionary<string, TextStyleSpec>(Custom, StringComparer.OrdinalIgnoreCase) { [name] = spec } };

    public static string SystemFamily =>
        OperatingSystem.IsWindows() ? "Segoe UI"
        : OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() ? "Helvetica Neue"
        : OperatingSystem.IsAndroid() ? "Roboto"
        : "sans-serif";
}
