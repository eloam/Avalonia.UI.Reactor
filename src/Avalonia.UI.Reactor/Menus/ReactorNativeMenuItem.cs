using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Menus;

using Avalonia.UI.Reactor.Views;

/// <summary>Élément de la barre du système (macOS) ; il retient la vue qu'il affiche (son clic exécute son action).</summary>
internal sealed class ReactorNativeMenuItem : NativeMenuItem
{
    /// <summary>Élément affiché ; null pour le titre d'un menu ou d'un sous-menu.</summary>
    public MenuItemView? Entry { get; set; }
}
