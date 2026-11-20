using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace Avalonia.UI.Reactor.Hosting;

/// <summary>
/// Application Avalonia d'un programme Reactor : styles des contrôles et fenêtre principale (aucune sans fabrique,
/// pour les tests). Nos contrôles portent leur propre apparence (<see cref="Widgets.ReactorThemes"/>) ; le thème Fluent
/// ne sert qu'à l'infrastructure : fenêtre, défilement dans un champ.
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
