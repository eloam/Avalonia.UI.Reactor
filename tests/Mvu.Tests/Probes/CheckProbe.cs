using Mvu.Primitives;
using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

/// <summary>Case à cocher avec modèle : carré rouge décoché, vert coché.</summary>
public partial class CheckProbe : Component
{
    [State] public partial bool Checked { get; set; }

    public override View Body => VStack(
        Checkbox(Checked, v => Checked = v).Template(s =>
            Box().Size(20, 20).Background(s.IsChecked ? Color.Hex("#00FF00") : Color.Hex("#FF0000"))));
}
