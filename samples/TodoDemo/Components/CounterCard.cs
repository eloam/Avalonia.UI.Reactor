using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace TodoDemo;

/// <summary>Composant autonome : cliquer sur « + » ne recalcule que cette carte.</summary>
public sealed partial class CounterCard : Component
{
    [State] partial int Count { get; set; }

    public override View Body =>
        Card(
            Text("Compteur").Font(TextStyle.Headline),
            HStack(
                Button("−", () => Count--).Style(ButtonStyle.Secondary),
                Text($"{Count}").FontSize(28).Bold().Width(72).TextAlignment(TextAlign.Center),
                Button("+", () => Count++),
                Spacer(),
                Count != 0 ? Button("Remettre à zéro", () => Count = 0).Style(ButtonStyle.Ghost) : null));
}
