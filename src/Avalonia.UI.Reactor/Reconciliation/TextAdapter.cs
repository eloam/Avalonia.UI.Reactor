using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary><c>Text</c> : un <see cref="TextBlock"/> d'une ligne, police et couleur du thème sauf modificateurs, terminé par « … » s'il manque de place.</summary>
internal sealed class TextAdapter : ViewAdapter<TextView, TextBlock>
{
    public static readonly TextAdapter Instance = new();

    protected override void Update(Reconciler reconciler, Node node, TextBlock text, TextView view, ViewEnvironment env)
    {
        text.Text = view.Content;
        text.SetFont(env.FontFor(view));
        text.SetBrush(TextBlock.ForegroundProperty, env.ForegroundFor(view));
        // Une ligne trop longue pour sa place se termine par « … », comme dans l'ancien moteur.
        text.TextTrimming = TextTrimming.CharacterEllipsis;
        text.TextAlignment = view.TextAlign switch
        {
            Primitives.TextAlign.Center => TextAlignment.Center,
            Primitives.TextAlign.End => TextAlignment.End,
            _ => TextAlignment.Start,
        };
    }
}
