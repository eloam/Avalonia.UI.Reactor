namespace Avalonia.UI.Reactor.Runtime;

/// <summary>Presse-papiers du système, fourni par l'hôte de fenêtre (SDL sur le bureau).</summary>
public interface IClipboard
{
    /// <summary>Texte du presse-papiers ; null s'il n'en contient pas.</summary>
    string? GetText();

    /// <summary>Remplace le contenu du presse-papiers par <paramref name="text"/>.</summary>
    void SetText(string text);
}
