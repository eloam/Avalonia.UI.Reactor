using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

/// <summary>Composant dont le Body est fourni par le test.</summary>
public class ViewProbe(Func<View> body) : Component
{
    public override View Body => body();
}
