namespace Avalonia.UI.Reactor.Headless.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Une pastille dessinée dont la couleur dépend d'un état.</summary>
public sealed partial class SwatchProbe : Component
{
    [State] public partial bool Red { get; set; }

    public override View Body =>
        VStack(
            Canvas(d => d.FillRect(d.Bounds, Red ? "#FF0000" : "#0000FF")).Size(40, 20),
            Button("Rouge", () => Red = true));
}
