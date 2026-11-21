// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents a push button.
/// </summary>
/// <param name="Label">The text of the button.</param>
/// <param name="OnClick">The action run when the button is clicked, or <see langword="null"/>.</param>
public sealed record ButtonView(string Label, Action? OnClick) : View
{
    /// <summary>
    /// Gets the built-in style of the button.
    /// </summary>
    public ButtonStyle Style { get; init; }

    /// <summary>
    /// Gets custom colors for the button; they take precedence over <see cref="Style"/>.
    /// </summary>
    public ButtonVariant? Variant { get; init; }

    /// <summary>
    /// Gets the name of a button style of the theme (<c>.Style("success")</c>); it takes precedence over
    /// <see cref="Style"/>.
    /// </summary>
    public string? StyleName { get; init; }

    /// <summary>
    /// Gets the custom look of this button; it takes precedence over the template of the theme.
    /// </summary>
    public Func<ButtonState, View>? CustomTemplate { get; init; }
}
