namespace Mvu.Views;

/// <summary>
/// Lettres d'accès des menus, à la manière de Windows : dans <c>"&amp;Fichier"</c>, la lettre F ouvre le menu
/// avec Alt+F ; <c>"&amp;&amp;"</c> affiche un « &amp; ».
/// </summary>
internal static class Mnemonics
{
    /// <summary>Texte à afficher et position de la lettre d'accès dans ce texte (-1 si aucune).</summary>
    public static (string Text, int Index) Parse(string label)
    {
        var text = new System.Text.StringBuilder(label.Length);
        int index = -1;
        for (int i = 0; i < label.Length; i++)
        {
            if (label[i] == '&' && i + 1 < label.Length)
            {
                if (label[i + 1] == '&') { text.Append('&'); i++; continue; }
                if (index < 0) index = text.Length;
                continue;
            }
            text.Append(label[i]);
        }
        return (text.ToString(), index);
    }

    /// <summary>La lettre d'accès de ce libellé correspond-elle à la touche ?</summary>
    public static bool Matches(string label, Primitives.KeyCode key)
    {
        var (text, index) = Parse(label);
        if (index < 0 || index >= text.Length) return false;
        char c = char.ToUpperInvariant(text[index]);
        return key switch
        {
            >= Primitives.KeyCode.A and <= Primitives.KeyCode.Z => c == (char)('A' + (key - Primitives.KeyCode.A)),
            >= Primitives.KeyCode.D0 and <= Primitives.KeyCode.D9 => c == (char)('0' + (key - Primitives.KeyCode.D0)),
            _ => false,
        };
    }
}
