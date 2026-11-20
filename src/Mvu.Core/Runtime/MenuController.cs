using Mvu.Layout;
using Mvu.Primitives;
using Mvu.Theming;
using Mvu.Views;

namespace Mvu.Runtime;

/// <summary>
/// Barre de menus dessinée : menus ouverts (dans la couche flottante de la fenêtre), navigation à la souris
/// et au clavier (F10, Alt, lettres d'accès, flèches), et raccourcis — ces derniers aussi en mode natif.
/// </summary>
internal sealed class MenuController(UiHost host)
{
    /// <summary><c>_path[0]</c> : menu de la barre ouvert ; puis, pour chaque sous-menu ouvert, sa position dans son parent.</summary>
    readonly List<int> _path = [];
    /// <summary>Élément surligné dans chaque menu ouvert (-1 : aucun).</summary>
    readonly List<int> _highlight = [];
    readonly List<LayoutNode> _overlays = [];
    LayoutNode? _barNode;
    int _focusedTitle;

    public MenuBarView? Bar { get; private set; }
    /// <summary>Barre activée au clavier (F10, Alt) : les lettres d'accès sont soulignées.</summary>
    public bool KeyboardMode { get; private set; }
    public bool IsOpen => _path.Count > 0;
    public bool IsActive => IsOpen || KeyboardMode;
    /// <summary>Menus ouverts, mis en page : dessinés par-dessus la fenêtre, testés en premier par le pointeur.</summary>
    public IReadOnlyList<LayoutNode> Overlays => _overlays;
    public bool ShowMnemonics => KeyboardMode;

    public bool IsTitleActive(int index) => IsOpen ? _path[0] == index : KeyboardMode && _focusedTitle == index;
    public bool IsHighlighted(int depth, int index) => depth < _highlight.Count && _highlight[depth] == index;

    /// <summary>Après chaque mise en page : retrouve la barre (sa vue a pu être recalculée) et replace les menus ouverts.</summary>
    public void Sync(IEnumerable<LayoutNode> nodes, bool native)
    {
        _barNode = nodes.FirstOrDefault(n => n.View is MenuBarView);
        Bar = _barNode?.View as MenuBarView;
        if (Bar is not { Menus.Count: > 0 } || native)
        {
            Close();
            return;
        }
        if (IsOpen && !PathIsValid()) Close();
        _focusedTitle = Math.Clamp(_focusedTitle, 0, Bar.Menus.Count - 1);
        Layout();
    }

    // ── Ouverture et fermeture ──

    public void Open(int index, bool keyboard)
    {
        if (Bar is null || index < 0 || index >= Bar.Menus.Count) return;
        _path.Clear();
        _highlight.Clear();
        _path.Add(index);
        _highlight.Add(keyboard ? NextEnabled(Bar.Menus[index], -1, +1) : -1);
        _focusedTitle = index;
        KeyboardMode = keyboard;
        Layout();
    }

    public void Close()
    {
        _path.Clear();
        _highlight.Clear();
        _overlays.Clear();
        KeyboardMode = false;
    }

    public void EnterKeyboardMode()
    {
        if (Bar is not { Menus.Count: > 0 }) return;
        Close();
        KeyboardMode = true;
        _focusedTitle = 0;
    }

    public bool OpenByMnemonic(KeyCode key)
    {
        if (Bar is null) return false;
        int index = IndexOfMnemonic(Bar.Menus.Select(m => m.Title), key);
        if (index < 0) return false;
        Open(index, keyboard: true);
        return true;
    }

    void OpenSubmenu(int depth, int index, bool keyboard)
    {
        Truncate(depth + 1);
        _highlight[depth] = index;
        _path.Add(index);
        _highlight.Add(keyboard ? NextEnabled(MenuAt(depth + 1), -1, +1) : -1);
        Layout();
    }

    void Truncate(int levels)
    {
        while (_path.Count > levels)
        {
            _path.RemoveAt(_path.Count - 1);
            _highlight.RemoveAt(_highlight.Count - 1);
        }
    }

    // ── Souris ──

    public void HoverTitle(int index)
    {
        if (IsOpen && _path[0] != index) Open(index, keyboard: false);
    }

    public void HoverRow(MenuRowView row)
    {
        if (row.Depth >= _path.Count) return;
        _highlight[row.Depth] = row.Index;
        bool submenuOpen = _path.Count > row.Depth + 1 && _path[row.Depth + 1] == row.Index;
        if (row.HasSubmenu && !submenuOpen) OpenSubmenu(row.Depth, row.Index, keyboard: false);
        else if (!row.HasSubmenu && _path.Count > row.Depth + 1)
        {
            Truncate(row.Depth + 1);
            Layout();
        }
    }

    public void Activate(MenuRowView row)
    {
        if (row.Item is MenuView)
        {
            OpenSubmenu(row.Depth, row.Index, keyboard: KeyboardMode);
            return;
        }
        if (row.Item is not MenuItemView { IsEnabled: true } item) return;
        Close();
        host.RunAction(item.Action);
    }

    // ── Clavier ──

    /// <summary>Touche reçue pendant que la barre est active. Faux : la touche n'est pas pour les menus.</summary>
    public bool KeyDown(KeyCode key, KeyModifiers modifiers)
    {
        if (Bar is not { Menus.Count: > 0 } bar) return false;
        if ((modifiers & (KeyModifiers.Ctrl | KeyModifiers.Meta)) != 0)
        {
            Close(); // un raccourci ferme les menus et s'exécute normalement
            return false;
        }
        int count = bar.Menus.Count;
        if ((modifiers & KeyModifiers.Alt) != 0 && key is >= KeyCode.A and <= KeyCode.Z)
        {
            OpenByMnemonic(key); // Alt + lettre : un autre menu de la barre
            return true;
        }
        if (key == KeyCode.Tab)
        {
            Close();
            return true;
        }

        if (!IsOpen)
        {
            switch (key)
            {
                case KeyCode.Left: _focusedTitle = (_focusedTitle + count - 1) % count; break;
                case KeyCode.Right: _focusedTitle = (_focusedTitle + 1) % count; break;
                case KeyCode.Down or KeyCode.Up or KeyCode.Enter or KeyCode.Space: Open(_focusedTitle, keyboard: true); break;
                case KeyCode.Escape: Close(); break;
                default: OpenByMnemonic(key); break;
            }
            return true;
        }

        int depth = _path.Count - 1;
        var menu = MenuAt(depth);
        int current = _highlight[depth];
        switch (key)
        {
            case KeyCode.Escape:
                if (depth > 0) { Truncate(depth); Layout(); }
                else Close();
                break;
            case KeyCode.Down: _highlight[depth] = NextEnabled(menu, current, +1); break;
            case KeyCode.Up: _highlight[depth] = NextEnabled(menu, current, -1); break;
            case KeyCode.Right:
                if (current >= 0 && menu.Items[current] is MenuView) OpenSubmenu(depth, current, keyboard: true);
                else Open((_path[0] + 1) % count, keyboard: true);
                break;
            case KeyCode.Left:
                if (depth > 0) { Truncate(depth); Layout(); }
                else Open((_path[0] + count - 1) % count, keyboard: true);
                break;
            case KeyCode.Enter or KeyCode.Space:
                if (current >= 0) Activate(new MenuRowView(menu.Items[current], depth, current));
                break;
            default:
                int index = IndexOfMnemonic(menu.Items.Select(LabelOf), key);
                if (index >= 0) Activate(new MenuRowView(menu.Items[index], depth, index));
                break;
        }
        return true;
    }

    /// <summary>
    /// Raccourci d'un élément de la barre, actif même menus fermés. Pendant une saisie de texte, seuls les raccourcis
    /// avec Ctrl, Alt, Cmd ou une touche de fonction s'appliquent (une simple lettre reste une lettre tapée).
    /// </summary>
    public bool TryShortcut(KeyCode key, KeyModifiers modifiers, bool editingText)
    {
        if (Bar is null || Find(Bar.Menus, key, modifiers) is not (var item, var gesture)) return false;
        if (editingText && !gesture.IsCommand) return false;
        Close();
        if (item.IsEnabled) host.RunAction(item.Action);
        return true;
    }

    /// <summary>Élément (et son raccourci) qui répond à cette touche, dans tous les menus et sous-menus.</summary>
    static (MenuItemView Item, KeyGesture Gesture)? Find(IEnumerable<View> items, KeyCode key, KeyModifiers modifiers)
    {
        foreach (var item in items)
        {
            var found = item switch
            {
                MenuItemView { Gesture: { } g } i when g.Matches(key, modifiers) => (i, g),
                MenuView m => Find(m.Items, key, modifiers),
                _ => null,
            };
            if (found is not null) return found;
        }
        return null;
    }

    // ── Mise en page des menus ouverts ──

    MenuView MenuAt(int depth)
    {
        var menu = (Bar ?? throw new InvalidOperationException("No menu is open.")).Menus[_path[0]];
        for (int d = 1; d <= depth; d++) menu = (MenuView)menu.Items[_path[d]];
        return menu;
    }

    bool PathIsValid()
    {
        if (Bar is null || _path[0] >= Bar.Menus.Count) return false;
        var menu = Bar.Menus[_path[0]];
        for (int d = 1; d < _path.Count; d++)
        {
            if (_path[d] >= menu.Items.Count || menu.Items[_path[d]] is not MenuView sub) return false;
            menu = sub;
        }
        for (int d = 0; d < _highlight.Count; d++)
            if (_highlight[d] >= MenuAt(d).Items.Count) _highlight[d] = -1;
        return true;
    }

    void Layout()
    {
        _overlays.Clear();
        if (!IsOpen || Bar is null || _barNode is null || _barNode.Children.Count <= _path[0]) return;
        var theme = _barNode.Theme.Menu;

        for (int depth = 0; depth < _path.Count; depth++)
        {
            bool below = depth == 0;
            var anchor = below ? _barNode.Children[_path[0]].Bounds : RowBounds(_overlays[depth - 1], _path[depth]);
            var panel = Panel(MenuAt(depth), depth, theme);
            _overlays.Add(host.LayoutOverlay(panel, _barNode, $"~menu{depth}", size => Place(anchor, size, below, theme)));
        }
    }

    static View Panel(MenuView menu, int depth, MenuTheme theme) =>
        Ui.VStack([.. menu.Items.Select((item, index) => item switch
            {
                DividerView => Ui.VStack(Ui.Divider()).Padding(0, theme.SeparatorSpacing),
                MenuItemView or MenuView => new MenuRowView(item, depth, index) { Width = float.PositiveInfinity },
                _ => (View?)null,
            })])
            .Spacing(0) with
        {
            Padding = theme.Padding,
            Background = theme.Background,
            BorderColor = theme.BorderColor,
            BorderWidth = 1,
            CornerRadius = theme.Radius,
        };

    static Rect RowBounds(LayoutNode panel, int index) =>
        panel.Children.FirstOrDefault(n => n.View is MenuRowView r && r.Index == index)?.Bounds ?? panel.Bounds;

    /// <summary>Sous le titre (menu de la barre) ou à droite de l'élément (sous-menu), sans sortir de la fenêtre.</summary>
    Rect Place(Rect anchor, Size size, bool below, MenuTheme theme)
    {
        var window = host.WindowSize;
        float w = Math.Max(size.Width, theme.MinWidth), h = size.Height;
        float x = below ? anchor.X : anchor.Right;
        float y = below ? anchor.Bottom : anchor.Y - theme.Padding.Top;
        if (!below && x + w > window.Width) x = anchor.X - w;
        if (y + h > window.Height) y = below ? anchor.Y - h : window.Height - h;
        return new Rect(Math.Clamp(x, 0, Math.Max(0, window.Width - w)), Math.Max(0, y), w, h);
    }

    // ── Outils ──

    static string LabelOf(View item) => item switch
    {
        MenuItemView i => i.Label,
        MenuView m => m.Title,
        _ => "",
    };

    static int IndexOfMnemonic(IEnumerable<string> labels, KeyCode key)
    {
        int i = 0;
        foreach (var label in labels)
        {
            if (label.Length > 0 && Mnemonics.Matches(label, key)) return i;
            i++;
        }
        return -1;
    }

    /// <summary>Élément sélectionnable suivant (ou précédent), en bouclant ; -1 s'il n'y en a aucun.</summary>
    static int NextEnabled(MenuView menu, int from, int direction)
    {
        int n = menu.Items.Count;
        for (int step = 1; step <= n; step++)
        {
            int i = ((from + direction * step) % n + n) % n;
            if (menu.Items[i] is MenuView or MenuItemView { IsEnabled: true }) return i;
        }
        return -1;
    }
}
