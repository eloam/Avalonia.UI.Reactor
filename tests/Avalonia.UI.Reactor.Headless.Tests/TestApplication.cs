using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Avalonia.UI.Reactor.Headless.Tests.TestApplication))]

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Hosting;

/// <summary>Application des tests : la même que celle d'un programme, sur la plateforme sans fenêtre d'Avalonia.</summary>
public static class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure(() => new ReactorApplication())
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
