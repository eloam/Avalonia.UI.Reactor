using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Widgets;

/// <summary>Bouton habillé par <see cref="ReactorThemes.Button"/> : ses couleurs par état viennent de l'adaptateur.</summary>
internal sealed class ReactorButton : Button
{
    public ReactorButton() => Theme = ReactorThemes.Button;
}
