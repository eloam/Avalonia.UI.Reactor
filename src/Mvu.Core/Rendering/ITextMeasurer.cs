using Mvu.Primitives;

namespace Mvu.Rendering;

public interface ITextMeasurer
{
    /// <summary>Taille en unités logiques (DIP) d'une ligne de texte.</summary>
    Size MeasureText(string text, Font font);
}
