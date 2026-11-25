// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Theming;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A button with a template set on the view, a checkbox and a field with templates of the theme (as in the Sable theme of the sample app).
/// </summary>
public sealed partial class TemplateProbe : Component
{
    [State] public partial int Clicks { get; set; }

    [State] public partial bool Done { get; set; }

    [State] public partial string Draft { get; set; } = "";

    /// <summary>
    /// Gets a theme whose checkboxes and fields have a template.
    /// </summary>
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
