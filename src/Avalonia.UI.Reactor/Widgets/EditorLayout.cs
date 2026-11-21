// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Represents the layout of a templated text field: the view of the template (frame, underline, label…) fills the
/// field, and the editing area of Avalonia (text, caret, selection, input methods) is placed exactly on the
/// <see cref="EditorSlot"/> of the template.
/// </summary>
internal sealed class EditorLayout : Panel
{
    // The view of the template is the first child; the editing area is the second.
    private Control? Look => Children.Count > 0 ? Children[0] : null;

    private Control? Editor => Children.Count > 1 ? Children[1] : null;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Editor is { } editor)
        {
            editor.Measure(Size.Infinity);
            if (SlotIn(Look) is { } slot)
            {
                slot.Natural = slot.Natural.WithHeight(editor.DesiredSize.Height);
            }
        }

        Look?.Measure(availableSize);
        return Look?.DesiredSize ?? default;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        Look?.Arrange(new Rect(finalSize));
        if (Editor is { } editor)
        {
            EditorSlot? slot = SlotIn(Look);
            Point? origin = slot?.TranslatePoint(default, this);
            editor.Arrange(slot is not null && origin is { } position ? new Rect(position, slot.Bounds.Size) : default);
        }

        return finalSize;
    }

    private static EditorSlot? SlotIn(Control? look) =>
        look is null ? null : look as EditorSlot ?? look.GetVisualDescendants().OfType<EditorSlot>().FirstOrDefault();
}
