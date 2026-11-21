// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Avalonia.UI.Reactor.Tests.TestApplication))]

namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Hosting;

/// <summary>
/// The test application: the same as that of a program, on the headless platform of Avalonia, with the real Skia
/// rendering (tests read the pixels of a window), done on the user interface thread: a capture sees everything
/// invalidated before it.
/// </summary>
public static class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure(() => new ReactorApplication())
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false, ShouldRenderOnUIThread = true });
}
