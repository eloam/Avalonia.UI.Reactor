namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary><c>field.Editor</c> dans le modèle d'un champ : un <see cref="EditorSlot"/>, à la largeur naturelle des champs du thème.</summary>
internal sealed class EditorAdapter : ViewAdapter<EditorView, EditorSlot>
{
    public static readonly EditorAdapter Instance = new();

    protected override void Update(Reconciler reconciler, Node node, EditorSlot slot, EditorView view, ViewEnvironment env) =>
        slot.Natural = slot.Natural.WithWidth(env.Theme.TextField.MinWidth);
}
