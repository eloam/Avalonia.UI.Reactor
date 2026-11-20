using Mvu.Runtime;
using Mvu.Views;

namespace Mvu.Tests.Probes;

/// <summary>Barre « du système » factice : garde la dernière barre reçue et permet de simuler un choix.</summary>
public sealed class FakeNativeMenuBar : INativeMenuBar
{
    public MenuBarView? Bar { get; private set; }
    public int Updates { get; private set; }
    Action<Action?>? _run;

    public void Update(MenuBarView? bar, Action<Action?> run)
    {
        Bar = bar;
        Updates++;
        _run = run;
    }

    /// <summary>L'utilisateur choisit un élément dans la barre du système.</summary>
    public void Choose(int menu, int item) => _run!(((MenuItemView)Bar!.Menus[menu].Items[item]).Action);
}
