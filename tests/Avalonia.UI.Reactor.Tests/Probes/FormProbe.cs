// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A field bound to state; Enter or "Ajouter" adds the text to the list and clears the field.
/// </summary>
public sealed partial class FormProbe : Component
{
    [State] public partial string Draft { get; set; } = "";

    public StateList<string> Items { get; } = [];

    public override View Body =>
        VStack(
            HStack(TextField(Bind.Draft, "Que faut-il faire ?", onSubmit: Add), Button("Ajouter", Add)),
            ForEach(Items, Text, item => item));

    private void Add()
    {
        if (string.IsNullOrWhiteSpace(Draft))
            return;
        Items.Add(Draft.Trim());
        Draft = "";
    }
}
