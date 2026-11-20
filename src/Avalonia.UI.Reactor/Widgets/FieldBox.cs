using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Champ de saisie : un <see cref="TextBox"/> dont la largeur naturelle ne dépend pas du texte saisi. Elle vient du
/// thème (<c>MinWidth</c>) : un long texte défile dans le champ au lieu de l'élargir et de décaler toute la ligne,
/// comme dans l'ancien moteur. Le champ s'élargit seulement par la mise en page (<c>.Grow()</c>, <c>.FillWidth()</c>).
/// </summary>
internal sealed class FieldBox : TextBox
{
    /// <summary>Habillé par le thème des <see cref="TextBox"/>.</summary>
    protected override Type StyleKeyOverride => typeof(TextBox);

    protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize).WithWidth(0);
}
