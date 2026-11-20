using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Menus;

/// <summary>
/// Barre de menus d'un <c>Body</c> : dessinée en haut de la fenêtre, ou confiée au système quand la plateforme en a une
/// (barre globale de macOS). La même description alimente les deux ; seule celle qui s'affiche compte.
/// </summary>
internal sealed class ReactorMenuBar : Menu
{
    /// <summary>Habillée comme un <see cref="Menu"/>.</summary>
    protected override Type StyleKeyOverride => typeof(Menu);

    /// <summary>La barre transmise au système, tant que la barre est dans une fenêtre.</summary>
    public NativeMenu SystemMenu { get; } = new();

    /// <summary>
    /// La barre du système affiche les menus et exécute elle-même les raccourcis : la barre dessinée ne prend alors
    /// ni place ni espacement.
    /// </summary>
    public bool UsesSystemBar { get; private set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is not { } window) return;
        NativeMenu.SetMenu(window, SystemMenu);
        UsesSystemBar = NativeMenu.GetIsNativeMenuExported(window);
        IsVisible = !UsesSystemBar;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (e.RootVisual is TopLevel window && ReferenceEquals(NativeMenu.GetMenu(window), SystemMenu)) NativeMenu.SetMenu(window, null);
        UsesSystemBar = false;
    }
}
