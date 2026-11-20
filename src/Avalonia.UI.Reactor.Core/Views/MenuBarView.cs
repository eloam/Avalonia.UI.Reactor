namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Barre de menus de la fenêtre. Dessinée en haut de la fenêtre, ou confiée au système
/// (menu Win32, menu global de macOS) selon le réglage de l'hôte — le code de l'application ne change pas.
/// </summary>
public sealed record MenuBarView(IReadOnlyList<MenuView> Menus) : View;
