namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.AvaloniaViews;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Un contrôle Avalonia (un curseur) accordé à un état lu dans sa mise à jour, et un choix de contrôle.</summary>
public sealed partial class NativeProbe : Component
{
    [State] public partial double Level { get; set; } = 10;

    [State] public partial bool UseProgress { get; set; }

    public int Evaluations { get; private set; }

    public override View Body
    {
        get
        {
            Evaluations++;
            return VStack(
                Native(() => new Avalonia.Controls.Slider { Maximum = 100 }, slider => slider.Value = Level).Width(200),
                UseProgress
                    ? Native(() => new Avalonia.Controls.ProgressBar())
                    : Native(() => new Avalonia.Controls.Calendar()));
        }
    }
}
