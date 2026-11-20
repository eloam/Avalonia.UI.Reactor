namespace Avalonia.UI.Reactor.Views;

/// <summary>Titre d'un menu dans la barre dessinée (créé par la mise en page, pas par l'application).</summary>
internal sealed record MenuTitleView(MenuBarView Bar, int Index) : View
{
    public MenuView Menu => Bar.Menus[Index];
}
