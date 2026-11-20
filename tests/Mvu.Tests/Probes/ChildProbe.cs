using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

public partial class ChildProbe : Component
{
    [State] partial int Clicks { get; set; }
    public override View Body => Button($"enfant {Clicks}", () => Clicks++);
}
