using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Avalonia.UI.Reactor.Tests.TestApplication))]

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Hosting;

/// <summary>
/// Application des tests : la même que celle d'un programme, sur la plateforme sans fenêtre d'Avalonia, avec le vrai
/// rendu Skia (les tests lisent les pixels d'une fenêtre), fait sur le thread d'interface : une capture voit tout ce
/// qui a été invalidé avant elle.
/// </summary>
public static class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure(() => new ReactorApplication())
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false, ShouldRenderOnUIThread = true });
}
