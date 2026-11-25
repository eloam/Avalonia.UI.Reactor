// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Media;

namespace MartletUI.Tests;

/// <summary>
/// Makes the colors of our themes comparable with Avalonia brushes.
/// </summary>
internal static class ColorAssertions
{
    public static Color ToAvaloniaColor(this Primitives.Color c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    public static Color? ColorOf(this IBrush? brush) => (brush as ISolidColorBrush)?.Color;
}
