// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Views;

/// <summary>
/// Represents a checkbox with an optional label.
/// </summary>
/// <param name="IsChecked">A value indicating whether the box is checked.</param>
/// <param name="OnChange">The action that receives the new value when the user toggles the box, or <see langword="null"/>.</param>
/// <param name="Label">The label of the checkbox, or <see langword="null"/>.</param>
public sealed record CheckboxView(bool IsChecked, Action<bool>? OnChange, string? Label) : View
{
    /// <summary>
    /// Gets the custom look of this checkbox; it takes precedence over the template of the theme.
    /// </summary>
    public Func<CheckboxState, View>? CustomTemplate { get; init; }
}
