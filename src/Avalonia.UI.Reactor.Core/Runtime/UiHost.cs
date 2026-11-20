using Avalonia.UI.Reactor.Layout;
using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Rendering;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Runtime;

/// <summary>
/// Cœur indépendant de la plateforme : reçoit des événements d'entrée bruts (coordonnées logiques),
/// gère survol / appui / focus / curseur texte, et dessine via un <see cref="IDrawingContext"/>.
/// Un hôte (SDL3 aujourd'hui, Android/iOS demain) ne fait que relayer ses événements ici.
/// </summary>
public sealed class UiHost : IDisposable, IScopeHost
{
    readonly View _rootView;
    readonly ITextMeasurer _measurer;
    readonly ViewTree _tree;
    LayoutNode? _root;
    int _updatePending;
    bool _layoutDirty;
    Size _size;
    Dictionary<string, LayoutNode> _index = [];
    readonly Action _themesChanged;
    bool _disposed;

    internal string? Hovered, Pressed, Focused;
    /// <summary>Barre de menus dessinée : menus ouverts, navigation, raccourcis.</summary>
    internal MenuController Menus { get; }
    MenuBarView? _sentBar;
    bool _altArmed, _swallowText;
    internal int Caret;
    internal bool CaretVisible = true;
    internal float TextScroll;

    /// <summary>Le backend doit redessiner la fenêtre.</summary>
    public event Action? InvalidateRequested;
    public CursorKind Cursor { get; private set; }

    /// <summary>Mode clair/sombre du système, fourni par l'hôte. Suivi par les vues qui n'imposent pas de mode.</summary>
    public ColorScheme SystemScheme
    {
        get => Themes.SystemScheme;
        set => Themes.SystemScheme = value;
    }

    /// <summary>Catalogue des thèmes ; le modifier (ou changer de mode système) refait la mise en page.</summary>
    public Themes Themes { get; }

    /// <summary>Thème de la racine de la fenêtre (fond, réglages globaux comme le clignotement du curseur).</summary>
    public Theme CurrentTheme => _root?.Theme ?? ViewEnvironment.Root(Themes).Theme;

    /// <summary>Couleur de fond de la fenêtre (fond de page du thème racine).</summary>
    public Color ClearColor => CurrentTheme.Palette.Page;

    /// <summary>Plus petit zoom de l'interface (50 %).</summary>
    public const float MinZoom = 0.5f;
    /// <summary>Plus grand zoom de l'interface (300 %).</summary>
    public const float MaxZoom = 3f;
    /// <summary>Pas des raccourcis de zoom (10 %).</summary>
    public const float ZoomStep = 0.1f;

    static readonly KeyGesture[] ZoomInGestures = [new(KeyCode.Plus, KeyModifiers.Ctrl), new(KeyCode.Equal, KeyModifiers.Ctrl)];
    static readonly KeyGesture ZoomOutGesture = new(KeyCode.Minus, KeyModifiers.Ctrl);
    static readonly KeyGesture ZoomResetGesture = new(KeyCode.D0, KeyModifiers.Ctrl);
    static readonly KeyGesture PasteGesture = new(KeyCode.V, KeyModifiers.Ctrl);

    /// <summary>
    /// Zoom de l'interface (1 = 100 %, de <see cref="MinZoom"/> à <see cref="MaxZoom"/>), appliqué par l'hôte en plus
    /// du DPI de l'écran. Accessibilité, ou préférence de l'utilisateur.
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set
        {
            float z = Math.Clamp(value, MinZoom, MaxZoom);
            if (z == _zoom) return;
            _zoom = z;
            Repaint(); // l'hôte recalcule la taille logique, ce qui refait la mise en page
        }
    }
    float _zoom = 1;

    /// <summary>Ctrl + / Ctrl − / Ctrl 0 (Cmd sur macOS) zooment, dézooment, reviennent à 100 % ; vrai par défaut.</summary>
    public bool ZoomShortcuts { get; set; } = true;

    /// <summary>Presse-papiers du système, fourni par l'hôte ; null : coller est sans effet.</summary>
    public IClipboard? Clipboard { get; set; }

    /// <summary>Un champ de texte a le focus : l'hôte fait clignoter le curseur ; sinon il peut dormir.</summary>
    public bool IsEditingText => Focused is not null;

    /// <summary>
    /// Barre de menus du système, fournie par l'hôte (menu Win32, menu global de macOS) ; null = barre dessinée.
    /// À fixer avant le premier rendu : la <c>MenuBar</c> du <c>Body</c> ne prend alors aucune place dans la fenêtre.
    /// </summary>
    public INativeMenuBar? NativeMenuBar
    {
        get => _nativeMenuBar;
        set
        {
            if (ReferenceEquals(value, _nativeMenuBar)) return;
            _nativeMenuBar = value;
            _sentBar = null;
            _tree.MarkAllDirty();
            _layoutDirty = true;
            RequestUpdate();
        }
    }
    INativeMenuBar? _nativeMenuBar;

    public UiHost(Component component, ITextMeasurer measurer, Themes? themes = null, Action? wake = null)
    {
        _rootView = component;
        _measurer = measurer;
        Themes = themes ?? Themes.Default;
        _tree = new ViewTree(this, measurer);
        Menus = new MenuController(this);
        Dispatcher = new UiDispatcher(wake ?? (() => { }), Invalidate, Report);
        _themesChanged = () =>
        {
            _layoutDirty = true;
            RequestUpdate();
        };
        Themes.Changed += _themesChanged;
        HotReload.Register(this);
    }

    /// <summary>
    /// Ferme l'interface : ses portées se désabonnent de leurs états et l'hôte se détache du catalogue de thèmes.
    /// Sans cela, un catalogue ou des états qui survivent à la fenêtre (plusieurs fenêtres, états partagés)
    /// la garderaient en mémoire. L'hôte de fenêtre l'appelle à la fermeture.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Themes.Changed -= _themesChanged;
        _tree.Dispose();
        Menus.Close();
        Editors.Clear();
        _index = [];
        _root = null;
        _nativeMenuBar = null;
        HotReload.Unregister(this);
    }

    /// <summary>
    /// Erreur non gérée dans une action (synchrone ou async) : l'application continue.
    /// Sans abonné, l'erreur est écrite sur la sortie d'erreur et dans le débogueur.
    /// </summary>
    public event Action<Exception>? UnhandledException;

    void Report(Exception e)
    {
        if (UnhandledException is { } handler) handler(e);
        else
        {
            Console.Error.WriteLine($"[Reactor] Unhandled exception: {e}");
            System.Diagnostics.Debug.WriteLine($"[Reactor] Unhandled exception: {e}");
        }
    }

    /// <summary>À installer comme SynchronizationContext du thread d'interface par l'hôte.</summary>
    public UiDispatcher Dispatcher { get; }

    /// <summary>Bilan de la dernière mise à jour : portées réévaluées, réutilisées, total.</summary>
    public UpdateStats LastUpdate => _tree.LastStats;

    internal void RunAction(Action? action) => Run(action);

    /// <summary>Exécute une action utilisateur puis met à jour ce qui en dépend.</summary>
    void Run(Action? action)
    {
        if (action is null) return;
        try { RunningAction.Run(this, action); }
        catch (Exception e) { Report(e); }
        Invalidate();
    }

    /// <summary>
    /// Un état observé a changé (n'importe quel thread). Sur le thread d'interface, l'action en cours
    /// ou le Drain de l'hôte appliquera la mise à jour ; ailleurs, on réveille la boucle de l'hôte.
    /// </summary>
    internal void RequestUpdate()
    {
        if (Interlocked.Exchange(ref _updatePending, 1) == 1) return;
        if (SynchronizationContext.Current != Dispatcher) Dispatcher.Post(static _ => { }, null);
    }

    void IScopeHost.RequestUpdate() => RequestUpdate();

    void IScopeHost.Report(Exception error) => Report(error);

    void IScopeHost.Reload() => Dispatcher.Post(static h => ((UiHost)h!).ReloadAll(), this);

    /// <summary>Hot reload : tout réévaluer (appelé sur le thread d'interface).</summary>
    internal void ReloadAll()
    {
        _tree.MarkAllDirty();
        _layoutDirty = true;
    }

    /// <summary>Applique les changements d'état en attente immédiatement (pour que les événements
    /// suivants, déjà en file, voient le nouvel état) puis demande un rendu.</summary>
    public void Invalidate()
    {
        bool pending = Interlocked.Exchange(ref _updatePending, 0) == 1;
        if (_size.Width > 0 && (pending || _layoutDirty || _root is null)) Rebuild();
        InvalidateRequested?.Invoke();
    }

    void Repaint() => InvalidateRequested?.Invoke();

    void Rebuild()
    {
        if (_disposed) return;
        _layoutDirty = false;
        _root = _tree.Build(_rootView, _size, Themes);
        _index = LayoutEngine.Flatten(_root).ToDictionary(n => n.Id);
        if (Focused is not null && !(_index.TryGetValue(Focused, out var f) && f.View is TextFieldView)) Focused = null;
        if (Editors.Keys.Any(id => !_index.ContainsKey(id))) // champs disparus (lignes supprimées d'une liste)
            foreach (var id in Editors.Keys.Where(id => !_index.ContainsKey(id)).ToList()) Editors.Remove(id);

        Menus.Sync(_index.Values, native: _nativeMenuBar is not null);
        if (_nativeMenuBar is { } native && !ReferenceEquals(Menus.Bar, _sentBar))
        {
            _sentBar = Menus.Bar;
            native.Update(_sentBar, Run);
        }
    }

    public void Paint(IDrawingContext ctx, Size size)
    {
        if (_disposed) return;
        if (_root is null || size != _size)
        {
            _size = size;
            Interlocked.Exchange(ref _updatePending, 0);
            Rebuild();
        }
        Renderer.Paint(_root!, ctx, this);
        foreach (var overlay in Menus.Overlays)
        {
            var menu = overlay.Theme.Menu;
            ctx.DrawShadow(overlay.Bounds, overlay.Radius, menu.ShadowColor, menu.ShadowBlur, menu.ShadowOffsetY);
            Renderer.Paint(overlay, ctx, this);
        }
    }

    /// <summary>Taille logique de la fenêtre au dernier rendu.</summary>
    internal Size WindowSize => _size;

    /// <summary>Zone d'édition de chaque champ au dernier rendu (sa zone de contenu, ou l'<c>Editor</c> de son modèle).</summary>
    internal readonly Dictionary<string, (Rect Area, Font Font)> Editors = [];

    /// <summary>Met en page la vue produite par le modèle d'un contrôle, dans les bornes de ce contrôle.</summary>
    internal LayoutNode LayoutTemplate(View template, LayoutNode owner)
    {
        var node = _tree.MeasureDetached(template, ViewTree.EnvOf(owner, Themes), owner.Id + "~");
        LayoutEngine.Arrange(node, owner.Bounds);
        return node;
    }

    /// <summary>
    /// Met en page une vue de la couche flottante (menu ouvert) avec l'environnement de <paramref name="owner"/> ;
    /// <paramref name="place"/> choisit ses bornes d'après sa taille naturelle.
    /// </summary>
    internal LayoutNode LayoutOverlay(View view, LayoutNode owner, string id, Func<Size, Rect> place)
    {
        var node = _tree.MeasureDetached(view, ViewTree.EnvOf(owner, Themes), id);
        LayoutEngine.Arrange(node, place(node.Desired));
        return node;
    }

    /// <summary>Élément sous le pointeur : d'abord dans les menus ouverts, puis dans la fenêtre.</summary>
    LayoutNode? Hit(Point p) => OverlayAt(p) is { } overlay ? LayoutEngine.HitTest(overlay, p) : RootHit(p);

    LayoutNode? RootHit(Point p) => _root is null ? null : LayoutEngine.HitTest(_root, p);

    LayoutNode? OverlayAt(Point p) => Menus.Overlays.LastOrDefault(o => o.Bounds.Contains(p));

    /// <summary>Éléments affichés et leur position (tests automatisés, outils de debug).</summary>
    public IEnumerable<(View View, Rect Bounds)> Elements =>
        _root is null ? [] : LayoutEngine.Flatten(_root).Concat(Menus.Overlays.SelectMany(LayoutEngine.Flatten))
            .Select(n => (n.View, n.Bounds));

    // ---------- Pointeur ----------

    public void PointerMove(Point p)
    {
        var hit = Hit(p);
        Cursor = hit?.View switch
        {
            null or MenuTitleView or MenuRowView => CursorKind.Arrow,
            TextFieldView => CursorKind.IBeam,
            _ => CursorKind.Hand,
        };
        bool menuChanged = false;
        if (Menus.IsOpen)
        {
            int before = Menus.Overlays.Count;
            if (hit?.View is MenuRowView row) { Menus.HoverRow(row); menuChanged = true; }
            else if (hit?.View is MenuTitleView title) { Menus.HoverTitle(title.Index); menuChanged = true; }
            menuChanged |= before != Menus.Overlays.Count;
        }
        if (hit?.Id != Hovered || menuChanged) { Hovered = hit?.Id; Repaint(); }
    }

    public void PointerLeave()
    {
        if (Hovered is null) return;
        Hovered = null;
        Repaint();
    }

    public void PointerDown(Point p)
    {
        _altArmed = false;
        if (Menus.IsOpen && MenuPointerDown(p)) return;
        if (Menus.KeyboardMode) Menus.Close();

        var hit = Hit(p);
        if (hit?.View is MenuTitleView title)
        {
            Menus.Open(title.Index, keyboard: false);
            Pressed = null;
            Repaint();
            return;
        }
        Pressed = hit?.Id;
        if (hit?.View is TextFieldView tf)
        {
            float scroll = Focused == hit.Id ? TextScroll : 0;
            if (Focused != hit.Id) TextScroll = 0;
            Focused = hit.Id;
            Caret = CaretFromX(tf, hit, p.X + scroll);
            CaretVisible = true;
        }
        else Focused = null;
        Repaint();
    }

    /// <summary>
    /// Clic menus ouverts : dans un menu, rien avant le relâchement ; sur le titre ouvert, il se referme ;
    /// sur un autre titre, celui-ci s'ouvre ; ailleurs, les menus se ferment et le clic s'arrête là.
    /// </summary>
    bool MenuPointerDown(Point p)
    {
        Pressed = null;
        if (OverlayAt(p) is null)
        {
            if (RootHit(p)?.View is MenuTitleView title && !Menus.IsTitleActive(title.Index))
                Menus.Open(title.Index, keyboard: false);
            else Menus.Close();
        }
        Repaint();
        return true;
    }

    public void PointerUp(Point p)
    {
        var hit = Hit(p);
        if (Menus.IsOpen)
        {
            // Comme sous Windows : on peut appuyer sur un titre, glisser jusqu'à l'élément et relâcher.
            Pressed = null;
            if (hit?.View is MenuRowView row) Menus.Activate(row);
            Repaint();
            return;
        }
        var pressed = Pressed;
        Pressed = null;
        if (hit is not null && hit.Id == pressed)
        {
            Run(hit.View switch
            {
                ButtonView { OnClick: { } click } => click,
                CheckboxView { OnChange: { } change } c => () => change(!c.IsChecked),
                _ => hit.View.TapAction,
            });
        }
        Repaint();
    }

    int CaretFromX(TextFieldView tf, LayoutNode node, float x)
    {
        var (area, font) = Editors.TryGetValue(node.Id, out var editor) ? editor : (node.Content, node.Font);
        float local = x - area.X;
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i <= tf.Value.Length; i++)
        {
            float d = Math.Abs(_measurer.MeasureText(tf.Value[..i], font).Width - local);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    // ---------- Clavier ----------

    (LayoutNode Node, TextFieldView Field)? FocusedField() =>
        Focused is not null && _index.TryGetValue(Focused, out var n) && n.View is TextFieldView tf ? (n, tf) : null;

    /// <summary>
    /// Touche enfoncée. Ordre de traitement : menus ouverts (ou barre active au clavier), raccourcis de la barre,
    /// activation de la barre dessinée (F10, Alt + lettre d'accès), zoom, puis le champ qui a le focus. Vrai : touche utilisée.
    /// </summary>
    public bool KeyDown(KeyCode key, KeyModifiers modifiers = KeyModifiers.None)
    {
        _swallowText = false;
        if (key == KeyCode.Alt)
        {
            // Alt seul, relâché sans autre touche, active la barre (voir KeyUp) ; Alt Gr (Ctrl + Alt) ne compte pas.
            _altArmed = (modifiers & ~KeyModifiers.Alt) == KeyModifiers.None;
            return Menus.IsActive;
        }
        _altArmed = false;

        bool custom = _nativeMenuBar is null;
        if (custom && Menus.IsActive && Menus.KeyDown(key, modifiers)) return MenuKey();
        if (Menus.TryShortcut(key, modifiers, IsEditingText))
        {
            _swallowText = true;
            return true;
        }
        if (custom && Menus.Bar is { Menus.Count: > 0 })
        {
            if (key == KeyCode.F10 && modifiers == KeyModifiers.None)
            {
                Menus.EnterKeyboardMode();
                return MenuKey();
            }
            if (modifiers == KeyModifiers.Alt && Menus.OpenByMnemonic(key)) return MenuKey();
        }
        return ZoomKey(key, modifiers) || FieldKey(key, modifiers);
    }

    bool ZoomKey(KeyCode key, KeyModifiers modifiers)
    {
        if (!ZoomShortcuts) return false;
        if (ZoomInGestures.Any(g => g.Matches(key, modifiers))) Zoom = MathF.Round(Zoom + ZoomStep, 1);
        else if (ZoomOutGesture.Matches(key, modifiers)) Zoom = MathF.Round(Zoom - ZoomStep, 1);
        else if (ZoomResetGesture.Matches(key, modifiers)) Zoom = 1;
        else return false;
        return true;
    }

    /// <summary>Touche prise par les menus : le caractère qu'elle produit éventuellement n'est pas tapé dans un champ.</summary>
    bool MenuKey()
    {
        _swallowText = true;
        Repaint();
        return true;
    }

    /// <summary>Touche relâchée : Alt seul active (ou quitte) la barre de menus dessinée, comme sous Windows.</summary>
    public bool KeyUp(KeyCode key, KeyModifiers modifiers = KeyModifiers.None)
    {
        if (key != KeyCode.Alt || !_altArmed) return false;
        _altArmed = false;
        if (_nativeMenuBar is not null || Menus.Bar is not { Menus.Count: > 0 }) return false;
        if (Menus.IsActive) Menus.Close();
        else Menus.EnterKeyboardMode();
        Repaint();
        return true;
    }

    /// <summary>La fenêtre perd le focus : les menus ouverts se ferment.</summary>
    public void WindowDeactivated()
    {
        _altArmed = false;
        if (!Menus.IsActive) return;
        Menus.Close();
        Repaint();
    }

    /// <summary>Tabulation entre les champs, puis édition du champ qui a le focus (dont coller).</summary>
    bool FieldKey(KeyCode key, KeyModifiers modifiers)
    {
        if (key == KeyCode.Tab)
        {
            MoveFocus((modifiers & KeyModifiers.Shift) != 0 ? -1 : 1);
            return true;
        }
        if (FocusedField() is not { } focused) return false;
        var tf = focused.Field;
        if (PasteGesture.Matches(key, modifiers))
        {
            // Champ d'une seule ligne : les fins de ligne collées deviennent des espaces.
            if (Clipboard?.GetText() is { Length: > 0 } pasted) Insert(tf, pasted.ReplaceLineEndings(" "));
            _swallowText = true;
            return true;
        }

        string v = tf.Value;
        int c = Math.Clamp(Caret, 0, v.Length);
        CaretVisible = true;
        switch (key)
        {
            case KeyCode.Backspace when c > 0: Caret = c - 1; Change(tf, v.Remove(c - 1, 1)); break;
            case KeyCode.Delete when c < v.Length: Caret = c; Change(tf, v.Remove(c, 1)); break;
            case KeyCode.Left: Caret = Math.Max(0, c - 1); Repaint(); break;
            case KeyCode.Right: Caret = Math.Min(v.Length, c + 1); Repaint(); break;
            case KeyCode.Home: Caret = 0; Repaint(); break;
            case KeyCode.End: Caret = v.Length; Repaint(); break;
            case KeyCode.Escape: Focused = null; Repaint(); break;
            case KeyCode.Enter:
                if (tf.OnSubmit is { } submit) { Run(submit); Caret = int.MaxValue; }
                break;
            default: return false;
        }
        return true;
    }

    public void TextInput(string text)
    {
        if (_swallowText || Menus.IsActive)
        {
            _swallowText = false;
            return;
        }
        if (FocusedField() is { } focused) Insert(focused.Field, text);
    }

    void Insert(TextFieldView tf, string text)
    {
        int c = Math.Clamp(Caret, 0, tf.Value.Length);
        Caret = c + text.Length;
        CaretVisible = true;
        Change(tf, tf.Value.Insert(c, text));
    }

    void Change(TextFieldView tf, string value)
    {
        if (tf.OnChange is { } change) Run(() => change(value));
        else Repaint(); // champ en lecture seule : la valeur ne change pas
    }

    void MoveFocus(int dir)
    {
        if (_root is null) return;
        var fields = LayoutEngine.Flatten(_root).Where(n => n.View is TextFieldView).ToList();
        if (fields.Count == 0) return;
        int i = fields.FindIndex(n => n.Id == Focused);
        var next = fields[i < 0 ? 0 : (i + dir + fields.Count) % fields.Count];
        Focused = next.Id;
        Caret = ((TextFieldView)next.View).Value.Length;
        TextScroll = 0;
        CaretVisible = true;
        Repaint();
    }

    /// <summary>À appeler périodiquement (~530 ms) pour faire clignoter le curseur.</summary>
    public void BlinkCaret()
    {
        if (Focused is null) return;
        CaretVisible = !CaretVisible;
        Repaint();
    }
}
