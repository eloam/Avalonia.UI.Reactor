using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Rendering;

public interface ITextMeasurer
{
    /// <summary>Taille en unités logiques (DIP) d'une ligne de texte.</summary>
    Size MeasureText(string text, Font font);
}
