// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Views;

/// <summary>
/// Represents what a text field template receives.
/// </summary>
/// <remarks>
/// The template draws the frame (background, border, label, icon…) and places <see cref="Editor"/> where the text and
/// the caret appear.
/// </remarks>
/// <example>
/// <code>
/// Template = f =&gt; VStack(
///         Text("Name").Font(TextStyle.Small),
///         f.Editor.Padding(0, 4),
///         Divider().Background(f.Focused ? ColorRole.Accent : ColorRole.Border))
///     .Spacing(2)
/// </code>
/// </example>
/// <param name="Value">The text of the field.</param>
/// <param name="Placeholder">The text shown while the field is empty.</param>
/// <param name="Focused">A value indicating whether the field has the keyboard focus.</param>
/// <param name="Hovered">A value indicating whether the pointer is over the field.</param>
public sealed record TextFieldState(string Value, string Placeholder, bool Focused, bool Hovered)
{
    /// <summary>
    /// Gets the editing area (text, caret, scrolling), managed by the framework.
    /// </summary>
    public EditorView Editor { get; } = new() { Width = float.PositiveInfinity };
}
