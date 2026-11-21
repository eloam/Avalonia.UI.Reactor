// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents a single-line text field.
/// </summary>
/// <param name="Value">The text shown by the field.</param>
/// <param name="OnChange">The action that receives the text typed by the user, or <see langword="null"/>.</param>
public sealed record TextFieldView(string Value, Action<string>? OnChange) : View
{
    /// <summary>
    /// Gets the text shown while the field is empty.
    /// </summary>
    public string Placeholder { get; init; } = string.Empty;

    /// <summary>
    /// Gets the action run when the user presses Enter, or <see langword="null"/>.
    /// </summary>
    public Action? OnSubmit { get; init; }

    /// <summary>
    /// Gets the custom look of this field; it takes precedence over the template of the theme.
    /// </summary>
    public Func<TextFieldState, View>? CustomTemplate { get; init; }
}
