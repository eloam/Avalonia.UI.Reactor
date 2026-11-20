namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;

/// <summary>Composant sans état qui affiche une vue donnée : pour vérifier la mise en page ou le thème d'une vue.</summary>
public sealed class ViewProbe(Func<View> body) : Component
{
    public override View Body => body();
}
