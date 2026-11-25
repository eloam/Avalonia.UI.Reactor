// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace MartletUI.Reconciliation;

/// <summary>
/// Converts our values (colors, thicknesses, fonts) into Avalonia values.
/// </summary>
internal static class Conversions
{
    // One instance per font family name; used on the user interface thread only.
    private static readonly Dictionary<string, FontFamily> s_families = [];

    /// <summary>
    /// Converts a color.
    /// </summary>
    /// <param name="color">Our color.</param>
    /// <returns>The Avalonia color.</returns>
    public static Color ToAvalonia(this Primitives.Color color) => Color.FromArgb(color.A, color.R, color.G, color.B);

    /// <summary>
    /// Converts a thickness.
    /// </summary>
    /// <param name="thickness">Our thickness.</param>
    /// <returns>The Avalonia thickness.</returns>
    public static Thickness ToAvalonia(this Primitives.Thickness thickness) =>
        new(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

    /// <summary>
    /// Sets a solid color on a brush property, without replacing a brush of the same color: a new brush at each update
    /// would redraw the control for nothing.
    /// </summary>
    /// <param name="target">The object that owns the property.</param>
    /// <param name="property">The brush property.</param>
    /// <param name="color">The color, or <see langword="null"/> to restore the default value of the property.</param>
    public static void SetBrush(this AvaloniaObject target, StyledProperty<IBrush?> property, Primitives.Color? color)
    {
        if (color is not { } solid)
        {
            target.ClearValue(property);
            return;
        }

        Color value = solid.ToAvalonia();
        if (target.GetValue(property) is ISolidColorBrush current && current.Color == value && current.Opacity == 1)
        {
            return;
        }

        target.SetValue(property, new ImmutableSolidColorBrush(value));
    }

    /// <summary>
    /// Sets the font of a text or a control, through the inherited properties of <see cref="TextElement"/>.
    /// </summary>
    /// <param name="target">The text or control.</param>
    /// <param name="font">The font.</param>
    public static void SetFont(this AvaloniaObject target, Primitives.Font font)
    {
        target.SetValue(TextElement.FontFamilyProperty, FamilyNamed(font.Family));
        target.SetValue(TextElement.FontSizeProperty, font.Size);
        target.SetValue(TextElement.FontWeightProperty, (FontWeight)font.Weight);
    }

    /// <summary>
    /// Returns the font family of a name, one instance per name.
    /// </summary>
    /// <param name="name">The name of the font family.</param>
    /// <returns>The font family.</returns>
    public static FontFamily FamilyNamed(string name)
    {
        if (!s_families.TryGetValue(name, out FontFamily? family))
        {
            family = new FontFamily(name);
            s_families[name] = family;
        }

        return family;
    }
}
