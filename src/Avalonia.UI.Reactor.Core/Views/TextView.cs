using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

public sealed record TextView(string Content) : View
{
    public TextAlign TextAlign { get; init; }
}
