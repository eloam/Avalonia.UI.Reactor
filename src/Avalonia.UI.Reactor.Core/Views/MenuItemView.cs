using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

/// <summary>Élément de menu : un libellé et l'action déclenchée au clic, par son raccourci ou au clavier.</summary>
public sealed record MenuItemView(string Label, Action? Action) : View
{
    /// <summary>Raccourci clavier, actif même menu fermé (<c>.Shortcut("Ctrl+N")</c>).</summary>
    public KeyGesture? Gesture { get; init; }

    /// <summary>Un élément désactivé est grisé : ni son clic ni son raccourci ne déclenchent l'action.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Coche affichée devant le libellé ; null = élément sans coche.</summary>
    public bool? IsChecked { get; init; }
}
