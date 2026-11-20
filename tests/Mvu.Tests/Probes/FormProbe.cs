using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

/// <summary>Formulaire dont les contrôles sont liés à ses états : TextField(Bind.Name), Checkbox(Bind.Agreed).</summary>
public partial class FormProbe : Component
{
    [State] public partial string Name { get; set; } = "";
    [State] public partial bool Agreed { get; set; }

    public override View Body => VStack(
        TextField(Bind.Name, "Nom"),
        Checkbox(Bind.Agreed, "J'accepte"));
}
