using System.Collections;
using Mvu.State;

namespace Mvu.Theming;

/// <summary>
/// Le catalogue des thèmes de l'application, regroupés en familles (un thème clair et/ou un sombre sous un nom).
/// Les vues y font référence par leur nom : <c>.Theme("Sable")</c> habille une vue — un bouton, un composant,
/// l'écran entier — en clair ou en sombre selon le mode en cours. La première famille habille l'application par défaut.
/// <code>
/// Desktop.Run&lt;App&gt;(new WindowOptions { Themes = new()
/// {
///     { "Standard", Theme.Light, Theme.Dark },   // la première famille : celle par défaut
///     { "Sable", sable, sableNuit },             // clair ou sombre selon le mode en cours
///     { "Contraste", contraste },                // un seul thème : toujours celui-là
/// }});
/// </code>
/// </summary>
public sealed class Themes : IEnumerable<ThemeFamily>
{
    static readonly ThemeFamily[] StandardOnly = [ThemeFamily.Standard];

    readonly Signal _signal = new();
    readonly List<ThemeFamily> _families = [];
    readonly HashSet<string> _warned = new(StringComparer.OrdinalIgnoreCase);
    ColorScheme _systemScheme;

    /// <summary>Catalogue à remplir avec <c>{ "Nom", clair, sombre }</c> ; laissé vide, c'est la famille Standard.</summary>
    public Themes() { }

    /// <summary>
    /// Thèmes donnés sans nom de famille : le premier clair et le premier sombre forment la famille par défaut,
    /// chacun des autres forme sa propre famille. <c>new(sable, sableNuit)</c> = une famille « Sable ».
    /// </summary>
    public Themes(params Theme[] themes)
    {
        var light = themes.FirstOrDefault(t => t.Scheme == ColorScheme.Light);
        var dark = themes.FirstOrDefault(t => t.Scheme == ColorScheme.Dark);
        if ((light ?? dark) is { } first)
            _families.Add(ReferenceEquals(light, Theme.Light) && ReferenceEquals(dark, Theme.Dark)
                ? ThemeFamily.Standard
                : new ThemeFamily(first.Name, light, dark));
        foreach (var t in themes)
            if (!ReferenceEquals(t, light) && !ReferenceEquals(t, dark))
                _families.Add(ThemeFamily.Of(t.Name, t));
    }

    /// <summary>La famille Standard : Clair et Sombre.</summary>
    public static Themes Default => new() { ThemeFamily.Standard };

    /// <summary>Un seul thème, quel que soit le mode du système.</summary>
    public static implicit operator Themes(Theme theme) => new(theme);

    /// <summary>Déclenché quand le catalogue ou le mode du système change (l'hôte refait la mise en page).</summary>
    public event Action? Changed;

    IReadOnlyList<ThemeFamily> List => _families.Count > 0 ? _families : StandardOnly;

    /// <summary>Familles du catalogue, dans l'ordre (pour construire un sélecteur, par exemple).</summary>
    public IReadOnlyList<ThemeFamily> Families { get { _signal.Read(); return List; } }

    /// <summary>Famille qui habille l'application tant qu'aucune vue n'en demande une autre : la première.</summary>
    public ThemeFamily DefaultFamily { get { _signal.Read(); return List[0]; } }

    /// <summary>Tous les thèmes, toutes familles confondues.</summary>
    public IReadOnlyList<Theme> All { get { _signal.Read(); return [.. List.SelectMany(f => f.Variants).Distinct()]; } }

    /// <summary>Mode clair/sombre du système, fourni par l'hôte.</summary>
    public ColorScheme SystemScheme
    {
        get { _signal.Read(); return _systemScheme; }
        set
        {
            if (value == _systemScheme) return;
            _systemScheme = value;
            NotifyChanged();
        }
    }

    /// <summary>Un thème par son nom, toutes familles confondues (insensible à la casse).</summary>
    public Theme? this[string name] => List.SelectMany(f => f.Variants).FirstOrDefault(t => NameIs(t.Name, name));

    /// <summary>Une famille par son nom (insensible à la casse).</summary>
    public ThemeFamily? FamilyNamed(string name) => List.FirstOrDefault(f => NameIs(f.Name, name));

    /// <summary>La famille qui contient ce thème, s'il est au catalogue.</summary>
    public ThemeFamily? FamilyOf(Theme theme) => List.FirstOrDefault(f => f.Contains(theme));

    // ── Construction : new() { { "Sable", sable, sableNuit }, { "Contraste", contraste } } ──

    /// <summary>Ajoute une famille d'un ou deux thèmes (un clair et un sombre, dans n'importe quel ordre).</summary>
    public void Add(string name, Theme theme, Theme? other = null) => Add(ThemeFamily.Of(name, theme, other));

    /// <summary>Ajoute un thème seul, comme famille portant son nom.</summary>
    public void Add(Theme theme) => Add(ThemeFamily.Of(theme.Name, theme));

    /// <summary>Ajoute une famille ; une famille du même nom est remplacée.</summary>
    public void Add(ThemeFamily family)
    {
        int i = _families.FindIndex(f => NameIs(f.Name, family.Name));
        if (i >= 0) _families[i] = family;
        else _families.Add(family);
        NotifyChanged();
    }

    public IEnumerator<ThemeFamily> GetEnumerator() => Families.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// N'existe pas : le thème se déclare dans la vue. Cette méthode remplace le <c>Select</c> de LINQ
    /// (le catalogue est énumérable) pour que le compilateur donne un message clair.
    /// </summary>
    [Obsolete("'Themes.Select' does not exist: themes are declared in the view. Use '.Theme(\"Sable\")' on a view " +
              "(or on the root view for the whole window) and '.ColorScheme(ColorScheme.Dark)' to force a color scheme, " +
              "and keep the user's choice in a [State] property.", error: true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void Select(string name) => throw new NotSupportedException();

    /// <summary>Nom demandé par une vue mais absent du catalogue : signalé une fois, la vue garde le thème hérité.</summary>
    internal void WarnUnknown(string name)
    {
        lock (_warned)
            if (!_warned.Add(name)) return;
        var message = $"[Mvu] Unknown theme '{name}'. Available theme families: {string.Join(", ", List.Select(f => f.Name))}.";
        Console.Error.WriteLine(message);
        System.Diagnostics.Debug.WriteLine(message);
    }

    static bool NameIs(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    void NotifyChanged()
    {
        _signal.Notify();
        Changed?.Invoke();
    }
}
