namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Ce que reçoit un modèle de champ de texte. Le modèle dessine le cadre (fond, bordure, libellé,
/// icône…) et place <see cref="Editor"/> là où le texte et le curseur doivent apparaître.
/// <code>
/// Template = f => VStack(
///         Text("Nom").Font(TextStyle.Small),
///         f.Editor.Padding(0, 4),
///         Divider().Background(f.Focused ? ColorRole.Accent : ColorRole.Border))
///     .Spacing(2)
/// </code>
/// </summary>
public sealed record TextFieldState(string Value, string Placeholder, bool Focused, bool Hovered)
{
    /// <summary>Zone d'édition (texte, curseur, défilement), gérée par le framework.</summary>
    public EditorView Editor { get; } = new() { Width = float.PositiveInfinity };
}
