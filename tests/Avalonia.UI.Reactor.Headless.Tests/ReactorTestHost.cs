using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Hosting;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Une vraie fenêtre Avalonia, sans écran : le composant y est affiché par le moteur réel et dessiné par Skia. Clics,
/// survol et saisie passent par les entrées de la plateforme, comme pour un utilisateur.
/// </summary>
internal sealed class ReactorTestHost : IDisposable
{
    readonly List<Exception> _errors = [];

    public ReactorTestHost(Component root, double width = 400, double height = 300, Themes? themes = null)
    {
        Host = new ReactorHost(root, themes ?? Themes.Default);
        Host.UnhandledException += _errors.Add;
        Window = new Window { Width = width, Height = height, Content = Host };
        Window.Show();
        Settle();
    }

    public Window Window { get; }

    public ReactorHost Host { get; }

    /// <summary>Portées réévaluées, reprises telles quelles et total à la dernière mise à jour.</summary>
    public UpdateStats Stats => Host.LastUpdate;

    /// <summary>Erreurs signalées par les actions, sans que l'application s'arrête.</summary>
    public IReadOnlyList<Exception> Errors => _errors;

    public IEnumerable<Control> Controls => Window.GetVisualDescendants().OfType<Control>();

    public Button ButtonLabeled(string label) => Controls.OfType<Button>().Single(b => Equals(b.Content, label));

    public bool HasButton(string label) => Controls.OfType<Button>().Any(b => Equals(b.Content, label));

    /// <summary>Le seul champ de saisie de la fenêtre.</summary>
    public TextBox Field => Controls.OfType<TextBox>().Single();

    /// <summary>Une case par son libellé (null : la case sans libellé).</summary>
    public CheckBox CheckBoxLabeled(string? label) => Controls.OfType<CheckBox>().Single(b => Equals(b.Content, label));

    /// <summary>Textes affichés par des vues <c>Text</c> (pas les libellés des boutons, cases et champs).</summary>
    public IReadOnlyList<TextBlock> Texts =>
        [.. Controls.OfType<TextBlock>().Where(t => !t.GetVisualAncestors().TakeWhile(a => a != Host).Any(a => a is TemplatedControl))];

    public TextBlock TextShowing(string text) => Texts.Single(t => t.Text == text);

    public bool HasText(string text) => Texts.Any(t => t.Text == text);

    /// <summary>Bornes d'un contrôle dans la fenêtre.</summary>
    public Rect BoundsOf(Control control) =>
        new(control.TranslatePoint(default, Window) ?? throw new InvalidOperationException("The control is not in the window."),
            control.Bounds.Size);

    public void ClickButton(string label) => Click(ButtonLabeled(label));

    public void Click(Control control) => ClickAt(BoundsOf(control).Center);

    public void ClickAt(Point point)
    {
        Window.MouseDown(point, MouseButton.Left);
        Window.MouseUp(point, MouseButton.Left);
        Settle();
    }

    /// <summary>Pointeur au-dessus du contrôle, sans cliquer.</summary>
    public void Hover(Control control)
    {
        Window.MouseMove(BoundsOf(control).Center);
        Settle();
    }

    /// <summary>Texte tapé dans le contrôle qui a le focus.</summary>
    public void Type(string text)
    {
        Window.KeyTextInput(text);
        Settle();
    }

    /// <summary>
    /// Touche des commandes de la plateforme (Cmd sur macOS, Ctrl ailleurs) : celle que l'utilisateur enfonce pour un
    /// raccourci déclaré <c>"Ctrl+…"</c>.
    /// </summary>
    public static RawInputModifiers Command =>
        Primitives.KeyGesture.CommandModifier == Primitives.KeyModifiers.Meta ? RawInputModifiers.Meta : RawInputModifiers.Control;

    public void Press(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPress(key, modifiers, physicalKey, null);
        Window.KeyRelease(key, modifiers, physicalKey, null);
        Settle();
    }

    /// <summary>Couleur d'un pixel de la fenêtre, après un rendu complet par Skia.</summary>
    public Color PixelAt(double x, double y)
    {
        using var frame = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The window has not been rendered.");
        using var buffer = frame.Lock();
        int value = Marshal.ReadInt32(buffer.Address, (int)y * buffer.RowBytes + (int)x * 4);
        byte c0 = (byte)value, c1 = (byte)(value >> 8), c2 = (byte)(value >> 16), a = (byte)(value >> 24);
        return buffer.Format == PixelFormat.Rgba8888 ? Color.FromArgb(a, c0, c1, c2) : Color.FromArgb(a, c2, c1, c0);
    }

    /// <summary>Exécute le travail en attente sur le thread d'interface (mises à jour planifiées, suites d'await), puis la mise en page.</summary>
    /// <summary>Laisse tourner le thread d'interface (suites d'await, minuteries) jusqu'à ce que la condition soit vraie.</summary>
    public void WaitUntil(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!condition())
        {
            if (Environment.TickCount64 > deadline) throw new TimeoutException("The condition was not met in time.");
            Thread.Sleep(10);
            Settle();
        }
    }

    public void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
    }

    public void Dispose()
    {
        Window.Close();
        Dispatcher.UIThread.RunJobs();
    }
}
