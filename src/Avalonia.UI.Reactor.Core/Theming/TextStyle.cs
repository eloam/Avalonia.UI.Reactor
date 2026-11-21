// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Specifies a semantic text style, like <c>.font(.title)</c> in SwiftUI; the theme gives each style its size and weight.
/// </summary>
public enum TextStyle
{
    /// <summary>
    /// Small text: captions and footnotes.
    /// </summary>
    Small,

    /// <summary>
    /// Body text, the default.
    /// </summary>
    Body,

    /// <summary>
    /// The heading of a section or a card.
    /// </summary>
    Headline,

    /// <summary>
    /// The title of a screen.
    /// </summary>
    Title,

    /// <summary>
    /// A large title, for the main header.
    /// </summary>
    LargeTitle,
}
