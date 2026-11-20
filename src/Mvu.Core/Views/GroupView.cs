namespace Mvu.Views;

/// <summary>
/// Plusieurs vues présentées comme une seule : dans une pile, ses enfants prennent place directement
/// parmi ceux de la pile — <c>VStack(Header(), ForEach(todos, Row), Footer())</c>.
/// Hors d'une pile, ou avec des modificateurs, un groupe s'empile verticalement.
/// </summary>
public sealed record GroupView(IReadOnlyList<View> Children) : View;
