namespace Mvu.Views;

/// <summary>
/// Ligne d'un menu ouvert (créée par le gestionnaire de menus) : <see cref="Item"/> est un
/// <see cref="MenuItemView"/> ou un sous-menu (<see cref="MenuView"/>) ; <see cref="Depth"/> est le niveau
/// du menu ouvert (0 = menu de la barre) et <see cref="Index"/> la position de l'élément dans ce menu.
/// </summary>
internal sealed record MenuRowView(View Item, int Depth, int Index) : View
{
    public bool HasSubmenu => Item is MenuView;
    public bool IsEnabled => Item is MenuView || Item is MenuItemView { IsEnabled: true };
    public string Label => Item switch
    {
        MenuView m => m.Title,
        MenuItemView i => i.Label,
        _ => "",
    };
}
