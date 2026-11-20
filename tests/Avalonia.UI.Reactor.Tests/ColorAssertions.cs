using Avalonia.Media;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>Couleurs comparables entre nos thèmes et les pinceaux d'Avalonia.</summary>
internal static class ColorAssertions
{
    public static Color ToAvaloniaColor(this Primitives.Color c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    public static Color? ColorOf(this IBrush? brush) => (brush as ISolidColorBrush)?.Color;
}
