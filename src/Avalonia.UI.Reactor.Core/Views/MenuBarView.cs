namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Barre de menus de la fenêtre. Dessinée en haut de la fenêtre, ou confiée à la barre du
/// système quand la plateforme en a une (macOS) — le code de l'application ne change pas.
/// </summary>
public sealed record MenuBarView(IReadOnlyList<MenuView> Menus) : View;
