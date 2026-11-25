// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace MartletUI.Hosting;

/// <summary>
/// Represents the Avalonia application of a Martlet program: control styles and main window.
/// </summary>
/// <remarks>
/// Our controls carry their own look (<see cref="Widgets.MartletThemes"/>); the Fluent theme only serves the
/// infrastructure: the window, scroll bars and open menus.
/// </remarks>
/// <param name="createMainWindow">The function that creates the main window, or <see langword="null"/> for none, as
/// in tests.</param>
internal sealed class MartletApplication(Func<Window>? createMainWindow = null) : Application
{
    /// <inheritdoc/>
    public override void Initialize() => Styles.Add(new FluentTheme());

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (createMainWindow is not null && ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = createMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
