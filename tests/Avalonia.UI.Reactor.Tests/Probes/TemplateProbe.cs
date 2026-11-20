namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Bouton à modèle posé sur la vue, case et champ à modèles du thème (comme ceux du thème Sable de la démo).</summary>
public sealed partial class TemplateProbe : Component
{
    [State] public partial int Clicks { get; set; }

    [State] public partial bool Done { get; set; }

    [State] public partial string Draft { get; set; } = "";

    /// <summary>Thème dont les cases et les champs ont un modèle.</summary>
    public static Theme Look { get; } = Theme.Light
        .WithCheckbox(c => c with { Template = box => Text(box.IsChecked ? "[x] fait" : "[ ] à faire") })
        .WithTextField(f => f with
        {
            Template = field => VStack(
                    field.Editor,
                    Divider().Height(2).Background(field.Focused ? ColorRole.Accent : ColorRole.Border))
                .Spacing(4),
        });

    public override View Body =>
        VStack(
            Button("OK", () => Clicks++).Template(b => Text(b.Hovered ? $"<{b.Label}>" : $"[{b.Label} {Clicks}]")),
            Checkbox(Bind.Done),
            TextField(Bind.Draft, "Note"));
}
