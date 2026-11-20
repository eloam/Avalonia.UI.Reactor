using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

public partial class ListProbe : Component
{
    public StateList<string> Items { get; } = ["a"];
    public override View Body => VStack(ForEach(Items, s => Text(s)));
}
