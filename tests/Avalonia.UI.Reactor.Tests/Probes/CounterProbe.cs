namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Compteur : un état, son texte et des boutons qui le modifient ; <paramref name="name"/> distingue deux compteurs.</summary>
public sealed partial class CounterProbe(string name = "") : Component
{
    [State] public partial int Count { get; set; }

    public override View Body =>
        HStack(
            Button($"{name}−", () => Count--),
            Text($"{name}{Count}"),
            Button($"{name}+", () => Count++),
            Count != 0 ? Button($"{name}Remettre à zéro", () => Count = 0) : null);
}
