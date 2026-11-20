using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Champ avec modèle : la vue du modèle (cadre, soulignement, libellé…) remplit le champ, et la zone d'édition d'Avalonia
/// (texte, curseur, sélection, IME) est posée exactement sur l'emplacement <see cref="EditorSlot"/> que le modèle a placé.
/// </summary>
internal sealed class EditorLayout : Panel
{
    /// <summary>La vue du modèle (premier enfant) et la zone d'édition (second enfant).</summary>
    Control? Look => Children.Count > 0 ? Children[0] : null;

    Control? Editor => Children.Count > 1 ? Children[1] : null;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Editor is { } editor)
        {
            editor.Measure(Size.Infinity);
            if (SlotIn(Look) is { } slot) slot.Natural = slot.Natural.WithHeight(editor.DesiredSize.Height);
        }
        Look?.Measure(availableSize);
        return Look?.DesiredSize ?? default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Look?.Arrange(new Rect(finalSize));
        if (Editor is { } editor)
        {
            var slot = SlotIn(Look);
            var origin = slot?.TranslatePoint(default, this);
            editor.Arrange(slot is not null && origin is { } at ? new Rect(at, slot.Bounds.Size) : default);
        }
        return finalSize;
    }

    static EditorSlot? SlotIn(Control? look) =>
        look is null ? null : look as EditorSlot ?? look.GetVisualDescendants().OfType<EditorSlot>().FirstOrDefault();
}
