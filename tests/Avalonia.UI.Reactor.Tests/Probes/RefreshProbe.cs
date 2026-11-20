namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Composant sans <c>[State]</c> : un champ ordinaire, affiché après un appel explicite à <c>Refresh</c>.</summary>
public sealed class RefreshProbe : Component
{
    int _value;

    public void Set(int value)
    {
        _value = value;
        Refresh();
    }

    public override View Body => Text($"{_value}");
}
