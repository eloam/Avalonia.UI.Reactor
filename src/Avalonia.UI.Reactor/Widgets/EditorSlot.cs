// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Represents the place of the editing area in a text field template (<c>field.Editor</c>).
/// </summary>
/// <remarks>
/// The slot is empty, as high as a line of text of the field and as wide as the natural width of the theme;
/// <see cref="EditorLayout"/> places the real editing area on it.
/// </remarks>
internal sealed class EditorSlot : Control
{
    /// <summary>
    /// Gets or sets the natural size of the slot: the width of the theme and the height of the editing area, provided by
    /// <see cref="EditorLayout"/>.
    /// </summary>
    public Size Natural
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            field = value;
            InvalidateMeasure();
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) => Natural;
}
