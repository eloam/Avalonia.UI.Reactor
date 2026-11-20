using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

public partial class FillProbe : Component
{
    [State] partial int Clicks { get; set; }
    public override View Body => Button($"{Clicks}", () => Clicks++).FillWidth();
}
