using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace Avalonia.UI.Reactor.Hosting;

/// <summary>
/// Application Avalonia d'un programme Reactor : styles des contrôles et fenêtre principale (aucune sans fabrique,
/// pour les tests). Le thème Fluent habille les contrôles en attendant nos propres thèmes de contrôles.
/// </summary>
internal sealed class ReactorApplication(Func<Window>? createMainWindow = null) : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (createMainWindow is not null && ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = createMainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
