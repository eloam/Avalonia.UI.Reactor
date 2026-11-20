using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

public partial class Probe : Component
{
    [State] public partial int N { get; set; }
    public override View Body => VStack(Text($"N = {N}"));
}
