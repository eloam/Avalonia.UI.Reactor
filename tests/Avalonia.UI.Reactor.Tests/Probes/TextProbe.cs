using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

public class TextProbe(string text) : Component
{
    public override View Body => Text(text);
}
