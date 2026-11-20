namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Liste dont chaque ligne est identifiée par son texte ; un bouton inverse l'ordre.</summary>
public sealed class ListProbe : Component
{
    public StateList<string> Items { get; } = ["a", "b", "c"];

    public override View Body =>
        VStack(
            ForEach(Items, item => Text(item), item => item),
            Button("Inverser", () =>
            {
                var reversed = Items.Reverse().ToList();
                Items.Clear();
                Items.AddRange(reversed);
            }));
}
