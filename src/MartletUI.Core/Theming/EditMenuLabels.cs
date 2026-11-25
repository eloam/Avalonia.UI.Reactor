// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Theming;

/// <summary>
/// Represents the labels of the context menu of a text field. An application translates them in its theme.
/// </summary>
/// <param name="Cut">The label of the command that cuts the selection.</param>
/// <param name="Copy">The label of the command that copies the selection.</param>
/// <param name="Paste">The label of the command that pastes the clipboard.</param>
public sealed record EditMenuLabels(string Cut, string Copy, string Paste)
{
    /// <summary>
    /// Gets the default labels, in English like the messages of the framework.
    /// </summary>
    public static readonly EditMenuLabels Default = new("Cut", "Copy", "Paste");
}
