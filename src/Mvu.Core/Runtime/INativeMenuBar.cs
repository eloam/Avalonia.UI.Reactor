using Mvu.Views;

namespace Mvu.Runtime;

/// <summary>
/// Barre de menus fournie par le système (menu Win32, menu global de macOS…), implémentée par l'hôte de fenêtre.
/// Quand l'hôte en fournit une, la <see cref="MenuBarView"/> du <c>Body</c> ne prend plus de place dans la fenêtre :
/// sa description est transmise ici après chaque mise à jour. Les raccourcis restent gérés par le framework.
/// </summary>
public interface INativeMenuBar
{
    /// <summary>
    /// Remplace le contenu de la barre (<paramref name="bar"/> null : aucune barre). Quand l'utilisateur choisit
    /// un élément, l'hôte appelle <paramref name="run"/> avec son action : elle s'exécute comme un clic de
    /// l'interface (vue mise à jour, erreurs signalées).
    /// </summary>
    void Update(MenuBarView? bar, Action<Action?> run);
}
