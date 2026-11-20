using Avalonia.Controls;

namespace Avalonia.UI.Reactor;

/// <summary>
/// Vues propres au moteur Avalonia, à importer avec <c>using static Avalonia.UI.Reactor.AvaloniaViews;</c>.
/// </summary>
public static class AvaloniaViews
{
    /// <summary>
    /// Un contrôle Avalonia dans une vue, quand le framework n'en a pas d'équivalent : <paramref name="create"/> le crée une
    /// fois à sa place ; <paramref name="update"/> l'accorde à l'état à chaque nouvelle version de la vue, et de nouveau
    /// quand un état qu'elle a lu change.
    /// <code>Native(() => new Calendar(), calendar => calendar.DisplayDate = Today)</code>
    /// </summary>
    public static NativeView Native<T>(Func<T> create, Action<T>? update = null) where T : Control
    {
        ArgumentNullException.ThrowIfNull(create);
        return new NativeView(typeof(T), () => create(), update is null ? null : control => update((T)control));
    }
}
