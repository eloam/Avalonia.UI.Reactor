// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Primitives;

/// <summary>
/// Specifies the alignment of a view along one axis.
/// </summary>
public enum Align
{
    /// <summary>
    /// The view is aligned to the start of the axis (left or top).
    /// </summary>
    Start,

    /// <summary>
    /// The view is centered on the axis.
    /// </summary>
    Center,

    /// <summary>
    /// The view is aligned to the end of the axis (right or bottom).
    /// </summary>
    End,

    /// <summary>
    /// The view fills the available space on the axis.
    /// </summary>
    Stretch,
}
