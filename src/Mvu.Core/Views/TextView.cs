using Mvu.Primitives;

namespace Mvu.Views;

public sealed record TextView(string Content) : View
{
    public TextAlign TextAlign { get; init; }
}
