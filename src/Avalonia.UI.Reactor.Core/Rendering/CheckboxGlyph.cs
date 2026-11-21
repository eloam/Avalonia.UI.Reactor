// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Rendering;

/// <summary>
/// Draws the standard look of a checkbox box; a template of the theme can replace it.
/// </summary>
internal static class CheckboxGlyph
{
    /// <summary>
    /// Draws a checkbox box.
    /// </summary>
    /// <remarks>
    /// A checked box has a filled background and a check mark, with the hover overlay under the pointer. An unchecked
    /// box has a background and a border, which takes the hover color under the pointer. The check mark scales with the
    /// size of the box.
    /// </remarks>
    /// <param name="context">The drawing context.</param>
    /// <param name="box">The bounds of the box.</param>
    /// <param name="theme">The theme of the checkbox.</param>
    /// <param name="isChecked">A value indicating whether the box is checked.</param>
    /// <param name="hovered">A value indicating whether the pointer is over the checkbox.</param>
    public static void Draw(IDrawingContext context, Rect box, Theme theme, bool isChecked, bool hovered)
    {
        CheckboxTheme style = theme.Checkbox;
        if (isChecked)
        {
            Color background = theme[style.CheckedBackground];
            context.FillRect(box, hovered ? theme.Overlay(background, style.HoverOverlay) : background, style.Radius);
            Point[] mark = Shapes.Fit(Shapes.CheckboxMark, box);
            float thickness = style.CheckThickness * box.Width / Shapes.CheckboxReferenceSize;
            context.DrawLine(mark[0], mark[1], theme[style.CheckMark], thickness);
            context.DrawLine(mark[1], mark[2], theme[style.CheckMark], thickness);
        }
        else
        {
            context.FillRect(box, theme[style.Background], style.Radius);
            context.StrokeRect(box, theme[hovered ? style.HoverBorderColor : style.BorderColor], style.BorderWidth, style.Radius);
        }
    }
}
