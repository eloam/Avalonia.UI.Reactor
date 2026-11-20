using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

public partial class ParentProbe : Component
{
    [State] partial int Clicks { get; set; }
    readonly ChildProbe child = new();
    public override View Body => VStack(Button($"parent {Clicks}", () => Clicks++), child);
}
