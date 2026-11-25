// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace MartletUI.Widgets;

/// <summary>
/// Represents a button dressed by <see cref="MartletThemes.Button"/>; its colors for each state come from its adapter.
/// </summary>
internal sealed class MartletButton : Button
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MartletButton"/> class.
    /// </summary>
    public MartletButton() => Theme = MartletThemes.Button;
}
