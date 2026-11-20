namespace Avalonia.UI.Reactor.Headless.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Champ dont l'état transforme la saisie (en majuscules) : le champ affiche la valeur de l'état.</summary>
public sealed partial class ShoutProbe : Component
{
    [State] public partial string Shout { get; set; } = "";

    public override View Body => VStack(TextField(Shout, value => Shout = value.ToUpperInvariant()));
}
