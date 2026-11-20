using Mvu.Primitives;
using Mvu.Views;

namespace Mvu.Theming;

/// <summary>
/// Boutons : les quatre styles connus (<see cref="ButtonStyle"/>) et des styles à soi, ajoutés par nom
/// (<c>.With("success", variant)</c>, utilisés par <c>.Style("success")</c>).
/// </summary>
public sealed record ButtonTheme(
    ButtonVariant Primary, ButtonVariant Secondary, ButtonVariant Ghost, ButtonVariant Danger,
    Thickness Padding, float Radius, float BorderWidth = 1, float? FontSize = null, bool Bold = false)
{
    /// <summary>
    /// Apparence sur mesure de tous les boutons de ce thème : une fonction qui reçoit le libellé, le style
    /// et l'état (survol, appui) et renvoie n'importe quelle vue. null = dessin standard.
    /// </summary>
    public Func<ButtonState, View>? Template { get; init; }

    /// <summary>Styles ajoutés par nom (insensible à la casse).</summary>
    public IReadOnlyDictionary<string, ButtonVariant> Custom { get; init; } =
        new Dictionary<string, ButtonVariant>(StringComparer.OrdinalIgnoreCase);

    public ButtonVariant this[ButtonStyle s] => s switch
    {
        ButtonStyle.Secondary => Secondary,
        ButtonStyle.Ghost => Ghost,
        ButtonStyle.Danger => Danger,
        _ => Primary,
    };

    /// <summary>Par nom : un style connu ("ghost"…) ou ajouté ; sinon le style principal.</summary>
    public ButtonVariant this[string name] =>
        Enum.TryParse<ButtonStyle>(name, ignoreCase: true, out var s) ? this[s]
        : Custom.TryGetValue(name, out var v) ? v
        : Primary;

    public ButtonTheme With(string name, ButtonVariant variant) =>
        Enum.TryParse<ButtonStyle>(name, ignoreCase: true, out var s)
            ? s switch
            {
                ButtonStyle.Secondary => this with { Secondary = variant },
                ButtonStyle.Ghost => this with { Ghost = variant },
                ButtonStyle.Danger => this with { Danger = variant },
                _ => this with { Primary = variant },
            }
            : this with { Custom = new Dictionary<string, ButtonVariant>(Custom, StringComparer.OrdinalIgnoreCase) { [name] = variant } };

    public static readonly ButtonTheme Default = new(
        Primary: new(ColorRole.Accent, ColorRole.OnAccent),
        Secondary: new(ColorRole.Secondary, ColorRole.OnSecondary),
        Ghost: new(Color.Transparent, ColorRole.TextMuted, HoverOverlay: 0.06f, PressedOverlay: 0.12f),
        Danger: new(ColorRole.Danger, ColorRole.OnDanger),
        Padding: new(14, 7), Radius: 6);
}
