// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Primitives;

/// <summary>
/// Specifies the modifier keys held with a key.
/// </summary>
[Flags]
public enum KeyModifiers
{
    /// <summary>
    /// No modifier key.
    /// </summary>
    None = 0,

    /// <summary>
    /// The Shift key.
    /// </summary>
    Shift = 1,

    /// <summary>
    /// The Control key. In a shortcut, <c>Ctrl</c> stands for the command key of the platform (Cmd on macOS).
    /// </summary>
    Ctrl = 2,

    /// <summary>
    /// The Alt key (Option on macOS).
    /// </summary>
    Alt = 4,

    /// <summary>
    /// The Command key on macOS, the Windows key elsewhere.
    /// </summary>
    Meta = 8,
}
