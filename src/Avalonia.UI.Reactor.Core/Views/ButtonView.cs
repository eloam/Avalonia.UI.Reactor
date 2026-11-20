using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Views;

public sealed record ButtonView(string Label, Action? OnClick) : View
{
    public ButtonStyle Style { get; init; }
    /// <summary>Variante sur mesure, prioritaire sur <see cref="Style"/>.</summary>
    public ButtonVariant? Variant { get; init; }
    /// <summary>Style nommé du thème (<c>.Style("success")</c>), prioritaire sur <see cref="Style"/>.</summary>
    public string? StyleName { get; init; }
    /// <summary>Apparence sur mesure pour ce bouton, prioritaire sur celle du thème.</summary>
    public Func<ButtonState, View>? CustomTemplate { get; init; }
}
