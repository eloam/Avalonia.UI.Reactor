// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Views;

/// <summary>
/// Represents what a checkbox template receives: the checked state, the label and the interaction state.
/// </summary>
/// <param name="IsChecked">A value indicating whether the box is checked.</param>
/// <param name="Label">The label of the checkbox, or <see langword="null"/>.</param>
/// <param name="Hovered">A value indicating whether the pointer is over the checkbox.</param>
/// <param name="Pressed">A value indicating whether the checkbox is pressed.</param>
public sealed record CheckboxState(bool IsChecked, string? Label, bool Hovered, bool Pressed);
