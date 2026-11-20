using Avalonia.Controls;

namespace Avalonia.UI.Reactor;

using Avalonia.UI.Reactor.Views;

/// <summary>
/// Un contrôle Avalonia quelconque placé dans une vue (<see cref="AvaloniaViews.Native{T}"/>) : il est créé une fois à sa
/// place, puis mis à jour à chaque nouvelle version de la vue. Les modificateurs s'y appliquent comme à toute vue.
/// </summary>
public sealed record NativeView : View
{
    internal NativeView(Type controlType, Func<Control> create, Action<Control>? update)
    {
        ControlType = controlType;
        Create = create;
        Update = update;
    }

    /// <summary>Type du contrôle : un autre type à la même place crée un nouveau contrôle.</summary>
    internal Type ControlType { get; }

    internal Func<Control> Create { get; }

    internal Action<Control>? Update { get; }
}
