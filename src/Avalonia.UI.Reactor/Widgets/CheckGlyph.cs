// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.Rendering;
using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents the box of a checkbox, drawn by <see cref="CheckboxGlyph"/> at the size of the theme.
/// </summary>
internal sealed class CheckGlyph : Control
{
    private Theme? _theme;
    private bool _checked;
    private bool _hovered;

    /// <summary>
    /// Shows the box in a state.
    /// </summary>
    /// <param name="theme">The theme of the checkbox.</param>
    /// <param name="isChecked">A value indicating whether the box is checked.</param>
    /// <param name="hovered">A value indicating whether the pointer is over the checkbox.</param>
    public void Show(Theme theme, bool isChecked, bool hovered)
    {
        if (!ReferenceEquals(theme, _theme))
        {
            Width = theme.Checkbox.Size;
            Height = theme.Checkbox.Size;
        }

        _theme = theme;
        _checked = isChecked;
        _hovered = hovered;
        InvalidateVisual();
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (_theme is null)
        {
            return;
        }

        using var drawing = new AvaloniaDrawingContext(context);
        CheckboxGlyph.Draw(drawing, new Primitives.Rect(0, 0, (float)Bounds.Width, (float)Bounds.Height), _theme, _checked, _hovered);
    }
}
