// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A clickable task row that contains a checkbox bound to the same state, and a frozen checkbox (without action).
/// </summary>
public sealed partial class TaskRowProbe : Component
{
    [State] public partial bool Done { get; set; }

    public override View Body =>
        VStack(
            HStack(Checkbox(Bind.Done), Text("tâche")).Padding(6).OnTap(() => Done = !Done),
            Checkbox(false, onChange: null, label: "figée"));
}
