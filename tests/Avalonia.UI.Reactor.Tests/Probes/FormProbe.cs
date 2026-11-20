namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Champ lié à un état ; Entrée ou « Ajouter » ajoute le texte à la liste et vide le champ.</summary>
public sealed partial class FormProbe : Component
{
    [State] public partial string Draft { get; set; } = "";

    public StateList<string> Items { get; } = [];

    public override View Body =>
        VStack(
            HStack(TextField(Bind.Draft, "Que faut-il faire ?", onSubmit: Add), Button("Ajouter", Add)),
            ForEach(Items, item => Text(item), item => item));

    void Add()
    {
        if (string.IsNullOrWhiteSpace(Draft)) return;
        Items.Add(Draft.Trim());
        Draft = "";
    }
}
