// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Theming;
using MartletUI.Views;
using static MartletUI.AvaloniaViews;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

/// <summary>
/// An Avalonia control (a slider) updated from a state, which gains and loses a background.
/// </summary>
public sealed partial class DecoratedNativeProbe : Component
{
    [State] public partial double Level { get; set; } = 10;

    [State] public partial bool Highlighted { get; set; }

    public int Updates { get; private set; }

    public override View Body
    {
        get
        {
            NativeView slider = Native(() => new Avalonia.Controls.Slider { Maximum = 100 }, Update).Width(200);
            return VStack(Highlighted ? slider.Background(ColorRole.Accent) : slider);
        }
    }

    private void Update(Avalonia.Controls.Slider slider)
    {
        Updates++;
        slider.Value = Level;
    }
}
