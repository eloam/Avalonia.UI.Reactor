using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.Views;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Avalonia.UI.Reactor.Desktop;

/// <summary>
/// Barre de menus native de Windows (HMENU) sous la barre de titre de la fenêtre SDL. Le menu n'est reconstruit
/// que si sa structure ou ses libellés changent ; un choix de l'utilisateur (<c>WM_COMMAND</c>, intercepté par
/// sous-classement de la fenêtre) exécute l'action sur le thread d'interface, après les événements en cours.
/// </summary>
[SupportedOSPlatform("windows6.0.6000")]
internal sealed unsafe class Win32MenuBar : INativeMenuBar, IDisposable
{
    /// <summary>Identifiant du premier élément ; les suivants sont numérotés dans l'ordre de la barre.</summary>
    const int FirstCommandId = 1000;
    /// <summary>Identifiant de notre sous-classement de la fenêtre (« Rctr » en ASCII), distinct de ceux d'autres bibliothèques.</summary>
    const nuint SubclassId = 0x52637472;

    readonly HWND _window;
    readonly Action<Action> _post;
    readonly List<Action?> _actions = [];
    GCHandle _self;
    HMENU _menu;
    string? _signature;
    Action<Action?>? _run;
    bool _subclassed;

    /// <param name="window">Fenêtre Win32 de SDL.</param>
    /// <param name="post">Exécute une action plus tard sur le thread d'interface (hors de la procédure de fenêtre).</param>
    public Win32MenuBar(HWND window, Action<Action> post)
    {
        _window = window;
        _post = post;
        _self = GCHandle.Alloc(this);
        _subclassed = PInvoke.SetWindowSubclass(window, &SubclassProc, SubclassId, (nuint)GCHandle.ToIntPtr(_self));
        if (!_subclassed) Console.Error.WriteLine("[Reactor] Failed to subclass the window; native menu commands will be ignored.");
    }

    public void Update(MenuBarView? bar, Action<Action?> run)
    {
        _run = run;
        var signature = new StringBuilder();
        _actions.Clear();
        if (bar is not null) Append(default, bar.Menus, signature); // passe à blanc : actions et signature
        if (signature.ToString() == _signature) return;
        _signature = signature.ToString();

        HMENU menu = default;
        if (bar is { Menus.Count: > 0 })
        {
            menu = PInvoke.CreateMenu();
            _actions.Clear();
            Append(menu, bar.Menus, new StringBuilder());
        }
        PInvoke.SetMenu(_window, menu);
        if (!_menu.IsNull) PInvoke.DestroyMenu(_menu);
        _menu = menu;
        PInvoke.DrawMenuBar(_window);
    }

    /// <summary>
    /// Ajoute les éléments à <paramref name="menu"/> (nul : rien n'est créé, seules la signature et les actions sont
    /// calculées) ; numérote les actions dans l'ordre de la barre.
    /// </summary>
    void Append(HMENU menu, IEnumerable<View> items, StringBuilder signature)
    {
        foreach (var item in items)
        {
            switch (item)
            {
                case DividerView:
                    signature.Append('|');
                    if (!menu.IsNull) AppendItem(menu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, null);
                    break;

                case MenuView sub:
                    signature.Append('>').Append(sub.Title).Append('{');
                    var popup = menu.IsNull ? default : PInvoke.CreatePopupMenu();
                    Append(popup, sub.Items, signature);
                    signature.Append('}');
                    if (!menu.IsNull) AppendItem(menu, MENU_ITEM_FLAGS.MF_POPUP, (nuint)(nint)popup.Value, sub.Title);
                    break;

                case MenuItemView entry:
                    int id = FirstCommandId + _actions.Count;
                    _actions.Add(entry.IsEnabled ? entry.Action : null);
                    // Le raccourci s'affiche à droite (après une tabulation) ; c'est le framework qui le traite.
                    var text = entry.Gesture is { } gesture ? $"{entry.Label}\t{gesture}" : entry.Label;
                    signature.Append(text).Append(entry.IsEnabled ? '+' : '-')
                        .Append(entry.IsChecked switch { true => '1', false => '0', null => '_' }).Append(';');
                    if (!menu.IsNull)
                    {
                        var flags = MENU_ITEM_FLAGS.MF_STRING;
                        if (!entry.IsEnabled) flags |= MENU_ITEM_FLAGS.MF_GRAYED;
                        if (entry.IsChecked == true) flags |= MENU_ITEM_FLAGS.MF_CHECKED;
                        AppendItem(menu, flags, (nuint)id, text);
                    }
                    break;
            }
        }
    }

    static void AppendItem(HMENU menu, MENU_ITEM_FLAGS flags, nuint id, string? text)
    {
        fixed (char* chars = text) PInvoke.AppendMenu(menu, flags, id, chars);
    }

    bool Command(int id)
    {
        int index = id - FirstCommandId;
        if (index < 0 || index >= _actions.Count || _run is not { } run) return false;
        var action = _actions[index];
        _post(() => run(action));
        return true;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    static LRESULT SubclassProc(HWND window, uint message, WPARAM wParam, LPARAM lParam, nuint subclassId, nuint referenceData)
    {
        // WM_COMMAND d'un menu : mot haut de wParam nul et lParam nul (sinon : contrôle enfant ou accélérateur).
        if (message == PInvoke.WM_COMMAND && ((wParam.Value >> 16) & 0xFFFF) == 0 && lParam.Value == 0
            && GCHandle.FromIntPtr((nint)referenceData).Target is Win32MenuBar bar && bar.Command((int)(wParam.Value & 0xFFFF)))
            return new LRESULT(0);
        return PInvoke.DefSubclassProc(window, message, wParam, lParam);
    }

    public void Dispose()
    {
        if (_subclassed) PInvoke.RemoveWindowSubclass(_window, &SubclassProc, SubclassId);
        _subclassed = false;
        if (!_menu.IsNull)
        {
            PInvoke.SetMenu(_window, default(HMENU));
            PInvoke.DestroyMenu(_menu);
            _menu = default;
        }
        if (_self.IsAllocated) _self.Free();
    }
}
