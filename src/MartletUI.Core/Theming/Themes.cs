// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using MartletUI.State;

namespace MartletUI.Theming;

/// <summary>
/// Represents the catalog of the themes of an application, grouped into families: a light and/or a dark theme under
/// one name.
/// </summary>
/// <remarks>
/// Views refer to themes by name: <c>.Theme("Sable")</c> dresses a view — a button, a component, the whole screen —
/// in light or dark depending on the current mode. The first family dresses the application by default.
/// </remarks>
/// <example>
/// <code>
/// MartletApp.Run&lt;App&gt;(new WindowOptions { Themes = new()
/// {
///     { "Standard", Theme.Light, Theme.Dark },   // the first family: the default one
///     { "Sable", sable, sableNight },            // light or dark depending on the current mode
///     { "Contrast", contrast },                  // a single theme: always this one
/// }});
/// </code>
/// </example>
public sealed class Themes : IEnumerable<ThemeFamily>
{
    private static readonly ThemeFamily[] s_standardOnly = [ThemeFamily.Standard];

    private readonly Signal _signal = new();
    private readonly List<ThemeFamily> _families = [];
    private readonly HashSet<string> _warned = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="Themes"/> class, to fill with <c>{ "Name", light, dark }</c>.
    /// </summary>
    /// <remarks>
    /// A catalog left empty contains the Standard family.
    /// </remarks>
    public Themes()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Themes"/> class from themes given without a family name.
    /// </summary>
    /// <remarks>
    /// The first light theme and the first dark theme form the default family, named after the light one; each other
    /// theme forms its own family. <c>new Themes(sable, sableNight)</c> is a single family named "Sable".
    /// </remarks>
    /// <param name="themes">The themes of the catalog.</param>
    public Themes(params Theme[] themes)
    {
        Theme? light = themes.FirstOrDefault(t => t.Scheme == ColorScheme.Light);
        Theme? dark = themes.FirstOrDefault(t => t.Scheme == ColorScheme.Dark);
        if ((light ?? dark) is { } first)
        {
            _families.Add(ReferenceEquals(light, Theme.Light) && ReferenceEquals(dark, Theme.Dark)
                ? ThemeFamily.Standard
                : new ThemeFamily(first.Name, light, dark));
        }

        foreach (Theme theme in themes)
        {
            if (!ReferenceEquals(theme, light) && !ReferenceEquals(theme, dark))
            {
                _families.Add(ThemeFamily.Of(theme.Name, theme));
            }
        }
    }

    /// <summary>
    /// Occurs when the catalog or the system color scheme changes; the host updates the views.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets a new catalog with the Standard family: the built-in Light and Dark themes.
    /// </summary>
    public static Themes Default => [ThemeFamily.Standard];

    /// <summary>
    /// Gets the families of the catalog, in order, for example to build a theme picker.
    /// </summary>
    public IReadOnlyList<ThemeFamily> Families
    {
        get
        {
            _signal.Read();
            return List;
        }
    }

    /// <summary>
    /// Gets the family that dresses the application as long as no view asks for another one: the first family.
    /// </summary>
    public ThemeFamily DefaultFamily
    {
        get
        {
            _signal.Read();
            return List[0];
        }
    }

    /// <summary>
    /// Gets all the themes of the catalog, from every family.
    /// </summary>
    public IReadOnlyList<Theme> All
    {
        get
        {
            _signal.Read();
            return [.. List.SelectMany(f => f.Variants).Distinct()];
        }
    }

    /// <summary>
    /// Gets or sets the light or dark mode of the system, provided by the host.
    /// </summary>
    public ColorScheme SystemScheme
    {
        get
        {
            _signal.Read();
            return field;
        }

        set
        {
            if (value == field)
            {
                return;
            }

            field = value;
            NotifyChanged();
        }
    }

    private IReadOnlyList<ThemeFamily> List => _families.Count > 0 ? _families : s_standardOnly;

    /// <summary>
    /// Gets a theme by name, from any family.
    /// </summary>
    /// <param name="name">The name of the theme, case-insensitive.</param>
    /// <returns>The theme, or <see langword="null"/> if the catalog does not contain it.</returns>
    public Theme? this[string name] => List.SelectMany(f => f.Variants).FirstOrDefault(t => NameIs(t.Name, name));

    /// <summary>
    /// Converts a single theme into a catalog: that theme is used whatever the system mode.
    /// </summary>
    /// <param name="theme">The theme.</param>
    public static implicit operator Themes(Theme theme) => new(theme);

    /// <summary>
    /// Returns a family by name.
    /// </summary>
    /// <param name="name">The name of the family, case-insensitive.</param>
    /// <returns>The family, or <see langword="null"/> if the catalog does not contain it.</returns>
    public ThemeFamily? FamilyNamed(string name) => List.FirstOrDefault(f => NameIs(f.Name, name));

    /// <summary>
    /// Returns the family that contains a theme.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <returns>The family, or <see langword="null"/> if the theme is not in the catalog.</returns>
    public ThemeFamily? FamilyOf(Theme theme) => List.FirstOrDefault(f => f.Contains(theme));

    /// <summary>
    /// Adds a family of one or two themes: a light and a dark one, in any order.
    /// </summary>
    /// <param name="name">The name of the family.</param>
    /// <param name="theme">A theme of the family.</param>
    /// <param name="other">The other theme of the family, or <see langword="null"/>.</param>
    public void Add(string name, Theme theme, Theme? other = null) => Add(ThemeFamily.Of(name, theme, other));

    /// <summary>
    /// Adds a single theme, as a family named after the theme.
    /// </summary>
    /// <param name="theme">The theme.</param>
    public void Add(Theme theme) => Add(ThemeFamily.Of(theme.Name, theme));

    /// <summary>
    /// Adds a family; a family with the same name is replaced.
    /// </summary>
    /// <param name="family">The family.</param>
    public void Add(ThemeFamily family)
    {
        int index = _families.FindIndex(f => NameIs(f.Name, family.Name));
        if (index >= 0)
        {
            _families[index] = family;
        }
        else
        {
            _families.Add(family);
        }

        NotifyChanged();
    }

    /// <summary>
    /// Returns an enumerator over the families of the catalog.
    /// </summary>
    /// <returns>An enumerator over the families.</returns>
    public IEnumerator<ThemeFamily> GetEnumerator() => Families.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Does not exist: themes are declared in the view.
    /// </summary>
    /// <remarks>
    /// The catalog is enumerable, so LINQ would offer its own <c>Select</c>; this member hides it so that the compiler
    /// gives a clear message instead.
    /// </remarks>
    /// <param name="name">Unused.</param>
    /// <exception cref="NotSupportedException">Always.</exception>
    [Obsolete("'Themes.Select' does not exist: themes are declared in the view. Use '.Theme(\"Sable\")' on a view " +
              "(or on the root view for the whole window) and '.ColorScheme(ColorScheme.Dark)' to force a color scheme, " +
              "and keep the user's choice in a [State] property.", error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Select(string name) => throw new NotSupportedException();

    /// <summary>
    /// Reports, once per name, a theme name asked for by a view but missing from the catalog; the view keeps its
    /// inherited theme.
    /// </summary>
    /// <param name="name">The unknown name.</param>
    internal void WarnUnknown(string name)
    {
        lock (_warned)
        {
            if (!_warned.Add(name))
            {
                return;
            }
        }

        string message = $"[Martlet] Unknown theme '{name}'. Available theme families: {string.Join(", ", List.Select(f => f.Name))}.";
        Console.Error.WriteLine(message);
        Debug.WriteLine(message);
    }

    private static bool NameIs(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private void NotifyChanged()
    {
        _signal.Notify();
        Changed?.Invoke();
    }
}
