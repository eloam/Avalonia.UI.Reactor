namespace Avalonia.UI.Reactor.Headless.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Ligne de tâche cliquable qui contient une case liée au même état, et une case figée (sans action).</summary>
public sealed partial class TaskRowProbe : Component
{
    [State] public partial bool Done { get; set; }

    public override View Body =>
        VStack(
            HStack(Checkbox(Bind.Done), Text("tâche")).Padding(6).OnTap(() => Done = !Done),
            Checkbox(false, onChange: null, label: "figée"));
}
