// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Media;
using MartletUI.Theming;
using MartletUI.Views;

namespace MartletUI.Reconciliation;

/// <summary>
/// Displays a <c>Text</c> view with a single-line <see cref="TextBlock"/>, in the font and color of the theme unless
/// modified, ending with an ellipsis when space runs out.
/// </summary>
internal sealed class TextAdapter : ViewAdapter<TextView, TextBlock>
{
    /// <summary>
    /// The single instance of the adapter.
    /// </summary>
    public static readonly TextAdapter Instance = new();

    /// <inheritdoc/>
    protected override void Update(Reconciler reconciler, Node node, TextBlock text, TextView view, ViewEnvironment environment)
    {
        text.Text = view.Content;
        text.SetFont(environment.FontFor(view));
        text.SetBrush(TextBlock.ForegroundProperty, environment.ForegroundFor(view));
        text.TextTrimming = TextTrimming.CharacterEllipsis;
        text.TextAlignment = view.TextAlign switch
        {
            Primitives.TextAlign.Center => TextAlignment.Center,
            Primitives.TextAlign.End => TextAlignment.End,
            _ => TextAlignment.Start,
        };
    }
}
