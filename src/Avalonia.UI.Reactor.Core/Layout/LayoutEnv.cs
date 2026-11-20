using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Layout;

/// <summary>Environnement hérité pendant la mesure : catalogue, famille et mode en cours, thème qui en découle, police, couleur de texte.</summary>
internal readonly record struct LayoutEnv(
    Themes Themes, ThemeFamily Family, ColorScheme Scheme, Theme Theme, Font Font, Color Foreground);
