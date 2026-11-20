namespace Avalonia.UI.Reactor.Theming;

/// <summary>Libellés du menu contextuel d'un champ (clic droit). Une application les traduit dans son thème.</summary>
/// <param name="Cut">Couper la sélection.</param>
/// <param name="Copy">Copier la sélection.</param>
/// <param name="Paste">Coller le presse-papiers.</param>
public sealed record EditMenuLabels(string Cut, string Copy, string Paste)
{
    /// <summary>Libellés par défaut, en anglais comme les messages du framework.</summary>
    public static readonly EditMenuLabels Default = new("Cut", "Copy", "Paste");
}
