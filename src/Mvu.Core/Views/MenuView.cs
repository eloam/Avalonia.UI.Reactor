namespace Mvu.Views;

/// <summary>
/// Un menu : un titre (<c>"&amp;Fichier"</c> : la lettre après <c>&amp;</c> l'ouvre avec Alt) et ses éléments —
/// <see cref="MenuItemView"/>, sous-menus (<see cref="MenuView"/>) et séparateurs (<c>Divider()</c>).
/// </summary>
public sealed record MenuView(string Title, IReadOnlyList<View> Items) : View;
