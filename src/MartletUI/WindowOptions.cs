// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.Theming;

namespace MartletUI;

/// <summary>
/// Represents the settings of the application window; set only what differs from the defaults.
/// </summary>
/// <example>
/// <code>
/// MartletApp.Run&lt;App&gt;(new WindowOptions { Title = "My app", Width = 1024, Themes = new(Theme.Light, sable) });
/// </code>
/// </example>
public sealed record WindowOptions
{
    /// <summary>
    /// Gets the title of the window.
    /// </summary>
    public string Title { get; init; } = "App";

    /// <summary>
    /// Gets the initial width, in device-independent units.
    /// </summary>
    public int Width { get; init; } = 800;

    /// <summary>
    /// Gets the initial height, in device-independent units.
    /// </summary>
    public int Height { get; init; } = 600;

    /// <summary>
    /// Gets the minimum width, in device-independent units.
    /// </summary>
    public int MinWidth { get; init; } = 320;

    /// <summary>
    /// Gets the minimum height, in device-independent units.
    /// </summary>
    public int MinHeight { get; init; } = 240;

    /// <summary>
    /// Gets a value indicating whether the user can resize the window.
    /// </summary>
    public bool Resizable { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the window has no title bar and no border from the system.
    /// </summary>
    public bool Borderless { get; init; }

    /// <summary>
    /// Gets a value indicating whether the window starts maximized.
    /// </summary>
    public bool Maximized { get; init; }

    /// <summary>
    /// Gets a value indicating whether the window starts in full screen.
    /// </summary>
    public bool Fullscreen { get; init; }

    /// <summary>
    /// Gets the theme catalog of the application, or <see langword="null"/> for the built-in Light and Dark themes.
    /// </summary>
    public Themes? Themes { get; init; }

    /// <summary>
    /// Gets the initial zoom of the interface, from 0.5 to 3 (1 is 100 %), on top of the scale factor of the display.
    /// </summary>
    public double Zoom { get; init; } = 1;

    /// <summary>
    /// Gets a value indicating whether Ctrl + / Ctrl − / Ctrl 0 (Cmd on macOS) zoom in, zoom out and reset the zoom.
    /// </summary>
    public bool ZoomShortcuts { get; init; } = true;
}
