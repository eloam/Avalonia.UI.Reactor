// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Represents a button dressed by <see cref="ReactorThemes.Button"/>; its colors for each state come from its adapter.
/// </summary>
internal sealed class ReactorButton : Button
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorButton"/> class.
    /// </summary>
    public ReactorButton() => Theme = ReactorThemes.Button;
}
