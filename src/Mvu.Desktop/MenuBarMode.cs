namespace Mvu.Desktop;

/// <summary>Apparence de la <c>MenuBar</c> déclarée dans le <c>Body</c>.</summary>
public enum MenuBarMode
{
    /// <summary>Barre du système sur macOS (menu global), barre dessinée ailleurs.</summary>
    Auto,
    /// <summary>Barre dessinée par le framework, en haut de la fenêtre : même apparence partout, suit le thème.</summary>
    Custom,
    /// <summary>
    /// Barre du système : menu Win32 sous la barre de titre sur Windows. Là où elle n'existe pas encore (macOS,
    /// Linux), la barre dessinée la remplace et un message l'indique sur la sortie d'erreur.
    /// </summary>
    Native,
}
