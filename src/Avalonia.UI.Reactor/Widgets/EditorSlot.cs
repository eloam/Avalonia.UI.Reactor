using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Emplacement de la zone d'édition dans le modèle d'un champ (<c>field.Editor</c>) : vide, à la hauteur d'une ligne de
/// texte du champ et à la largeur naturelle du thème. <see cref="EditorLayout"/> y pose la vraie zone d'édition.
/// </summary>
internal sealed class EditorSlot : Control
{
    Size _natural;

    /// <summary>Taille naturelle : largeur du thème, hauteur de la zone d'édition (fournie par <see cref="EditorLayout"/>).</summary>
    public Size Natural
    {
        get => _natural;
        set
        {
            if (value == _natural) return;
            _natural = value;
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize) => _natural;
}
