// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A block whose view methods get their own scopes: one reads its own state, one takes an argument, one reads a plain
/// field announced by <c>Refresh()</c>, one has a modifier, one holds a field; and an <c>Observe</c> block.
/// </summary>
public sealed partial class ViewMethodProbe : Component
{
    private string _note = "note 1";

    [State] public partial int Title { get; set; }

    [State] public partial int Progress { get; set; }

    [State] public partial int Size { get; set; }

    [State] public partial bool HasError { get; set; }

    [State] public partial string Name { get; set; } = "";

    // The number of evaluations of the Body, which the view methods do not cause.
    public int BodyRuns { get; private set; }

    public override View Body
    {
        get
        {
            BodyRuns++;
            return VStack(() =>
            {
                Text($"Titre {Title}");
                if (HasError)
                    Text("Erreur");

                ProgressView();
                Badge(Size % 2);
                Note().Bold();
                Form();
                Observe(() =>
                {
                    Text($"Observé {Progress}");
                    if (Progress > 1)
                        Text("Beaucoup");
                });
            });
        }
    }

    public void ChangeNote(string note)
    {
        _note = note;
        Refresh();
    }

    private static View Badge(int parity) => Text($"Parité {parity}");

    private View ProgressView() => Text($"Progression {Progress}");

    private View Note() => Text(_note);

    private View Form() => TextField(Bind.Name, "Nom");
}
