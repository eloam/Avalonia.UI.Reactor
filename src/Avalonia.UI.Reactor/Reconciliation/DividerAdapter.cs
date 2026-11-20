using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// <c>Divider()</c> : un trait plein, de l'épaisseur et de la couleur du thème (<c>DividerThickness</c>,
/// <c>DividerColor</c>) ; <c>.Height(…)</c> et <c>.Background(…)</c> les remplacent.
/// </summary>
internal sealed class DividerAdapter : ViewAdapter<DividerView, Border>
{
    public static readonly DividerAdapter Instance = new();

    /// <summary>Son fond est sa couleur : pas d'enveloppe.</summary>
    public override bool DrawsDecoration => true;

    protected override void Update(Reconciler reconciler, Node node, Border line, DividerView view, ViewEnvironment env)
    {
        var theme = env.Theme;
        line.SetBrush(Border.BackgroundProperty, theme[view.Background ?? theme.DividerColor]);
        // Hauteur naturelle : l'épaisseur du thème, sauf hauteur explicite (posée ensuite avec la taille de la vue).
        line.MinHeight = view.Height is null ? theme.DividerThickness : 0;
    }
}
