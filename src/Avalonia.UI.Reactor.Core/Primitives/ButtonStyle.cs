// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Primitives;

/// <summary>
/// Specifies the built-in style of a button, whose colors come from the theme.
/// </summary>
public enum ButtonStyle
{
    /// <summary>
    /// The main action of a screen, in the accent color.
    /// </summary>
    Primary,

    /// <summary>
    /// A secondary action, on a neutral background.
    /// </summary>
    Secondary,

    /// <summary>
    /// A discreet action, without background until hovered.
    /// </summary>
    Ghost,

    /// <summary>
    /// A destructive action, in the danger color.
    /// </summary>
    Danger,
}
