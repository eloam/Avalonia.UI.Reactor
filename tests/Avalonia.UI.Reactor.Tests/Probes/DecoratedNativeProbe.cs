// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.AvaloniaViews;
using static Avalonia.UI.Reactor.Views.Ui;

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
