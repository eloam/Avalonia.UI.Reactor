using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Hosting;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Une vraie fenêtre Avalonia, sans écran : le composant y est affiché par le moteur réel, et les clics passent par
/// les entrées de la plateforme (pointeur enfoncé puis relâché au centre du contrôle), comme pour un utilisateur.
/// </summary>
internal sealed class ReactorTestHost : IDisposable
{
    public ReactorTestHost(Component root, double width = 400, double height = 300, Themes? themes = null)
    {
        Host = new ReactorHost(root, themes ?? Themes.Default);
        Window = new Window { Width = width, Height = height, Content = Host };
        Window.Show();
        Settle();
    }

    public Window Window { get; }

    public ReactorHost Host { get; }

    /// <summary>Portées réévaluées, reprises telles quelles et total à la dernière mise à jour.</summary>
    public UpdateStats Stats => Host.LastUpdate;

    public IEnumerable<Control> Controls => Window.GetVisualDescendants().OfType<Control>();

    public Button ButtonLabeled(string label) => Controls.OfType<Button>().Single(b => Equals(b.Content, label));

    public bool HasButton(string label) => Controls.OfType<Button>().Any(b => Equals(b.Content, label));

    /// <summary>Textes affichés par des vues <c>Text</c> (pas les libellés des boutons).</summary>
    public IReadOnlyList<TextBlock> Texts =>
        [.. Controls.OfType<TextBlock>().Where(t => t.FindAncestorOfType<Button>() is null)];

    public TextBlock TextShowing(string text) => Texts.Single(t => t.Text == text);

    public bool HasText(string text) => Texts.Any(t => t.Text == text);

    /// <summary>Bornes d'un contrôle dans la fenêtre.</summary>
    public Rect BoundsOf(Control control) =>
        new(control.TranslatePoint(default, Window) ?? throw new InvalidOperationException("The control is not in the window."),
            control.Bounds.Size);

    public void ClickButton(string label) => Click(ButtonLabeled(label));

    public void Click(Control control)
    {
        var center = BoundsOf(control).Center;
        Window.MouseDown(center, MouseButton.Left);
        Window.MouseUp(center, MouseButton.Left);
        Settle();
    }

    /// <summary>Exécute le travail en attente sur le thread d'interface (mises à jour planifiées), puis la mise en page.</summary>
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
