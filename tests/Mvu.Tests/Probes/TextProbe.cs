using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

public class TextProbe(string text) : Component
{
    public override View Body => Text(text);
}
