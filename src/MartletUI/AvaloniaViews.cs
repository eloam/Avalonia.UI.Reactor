// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace MartletUI;

/// <summary>
/// Provides the views that are specific to the Avalonia engine; import them with
/// <c>using static MartletUI.AvaloniaViews;</c>.
/// </summary>
public static class AvaloniaViews
{
    /// <summary>
    /// Embeds an Avalonia control in a view, when the framework has no equivalent:
    /// <c>Native(() =&gt; new Calendar(), calendar =&gt; calendar.DisplayDate = Today)</c>.
    /// </summary>
    /// <remarks>
    /// The control is created once at its place. The update runs on each new version of the view, and again when some
    /// state it read changes. Modifiers apply to the control as to any view.
    /// </remarks>
    /// <typeparam name="T">The type of the control.</typeparam>
    /// <param name="create">The function that creates the control.</param>
    /// <param name="update">The function that updates the control from the state, or <see langword="null"/>.</param>
    /// <returns>The native view.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="create"/> is <see langword="null"/>.</exception>
    public static NativeView Native<T>(Func<T> create, Action<T>? update = null)
        where T : Control
    {
        ArgumentNullException.ThrowIfNull(create);
        return new NativeView(typeof(T), () => create(), update is null ? null : control => update((T)control));
    }
}
