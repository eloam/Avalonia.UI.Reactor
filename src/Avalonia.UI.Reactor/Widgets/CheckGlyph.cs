using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.Rendering;
using Avalonia.UI.Reactor.Theming;

/// <summary>La case d'une case à cocher, dessinée comme dans l'ancien moteur (<see cref="CheckboxGlyph"/>), à la taille du thème.</summary>
internal sealed class CheckGlyph : Control
{
    Theme? _theme;
    bool _checked, _hovered;

    public void Show(Theme theme, bool isChecked, bool hovered)
    {
        if (!ReferenceEquals(theme, _theme)) Width = Height = theme.Checkbox.Size;
        _theme = theme;
        _checked = isChecked;
        _hovered = hovered;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_theme is null) return;
        using var drawing = new AvaloniaDrawingContext(context);
        CheckboxGlyph.Draw(drawing, new Primitives.Rect(0, 0, (float)Bounds.Width, (float)Bounds.Height), _theme, _checked, _hovered);
    }
}
