using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Rendering;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.State;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// DSL : à importer avec <c>using static Avalonia.UI.Reactor.Views.Ui;</c> — plus aucun <c>new</c> dans les vues.
/// Les fabriques ne fixent aucun style : tout vient du thème, surchargeable par modificateur.
/// Les enfants viennent toujours en premier ; espacement, alignement… se règlent par modificateur.
/// </summary>
public static class Ui
{
    public static TextView Text(string content) => new(content);

    public static ButtonView Button(string label, Action? action = null) => new(label, action);

    /// <summary>Action asynchrone : <c>Button("Charger", LoadAsync)</c> ou <c>Button("…", async () => …)</c>.
    /// Une exception est remontée à <see cref="UiHost.UnhandledException"/> au lieu de fermer l'application.</summary>
    public static ButtonView Button(string label, Func<Task> action) => new(label, Async(action));

    /// <summary>Lance une tâche depuis le thread d'interface ; ses erreurs sont signalées à l'hôte.</summary>
    internal static Action Async(Func<Task> action) => async () =>
    {
        try { await action(); }
        catch (Exception e) when (SynchronizationContext.Current is UiDispatcher dispatcher) { dispatcher.Report(e); }
    };

    /// <summary>Champ lié à un état : <c>TextField(Bind.Draft, "Nom")</c> lit et modifie <c>Draft</c>.</summary>
    public static TextFieldView TextField(Binding<string> text, string placeholder = "", Action? onSubmit = null) =>
        TextField(text.Value, text.Set, placeholder, onSubmit);

    /// <summary>Champ à valeur calculée ou à traitement particulier : la valeur, puis ce qu'il faut faire quand elle change.</summary>
    public static TextFieldView TextField(string value, Action<string>? onChange,
        string placeholder = "", Action? onSubmit = null) =>
        new(value, onChange) { Placeholder = placeholder, OnSubmit = onSubmit };

    /// <summary>Case liée à un état : <c>Checkbox(todo.Bind.Done)</c>.</summary>
    public static CheckboxView Checkbox(Binding<bool> isChecked, string? label = null) =>
        Checkbox(isChecked.Value, isChecked.Set, label);

    public static CheckboxView Checkbox(bool isChecked, Action<bool>? onChange = null, string? label = null) =>
        new(isChecked, onChange, label);

    /// <summary>
    /// Empile verticalement. Espacement du thème (<c>Theme.Spacing</c>) sauf <c>.Spacing(…)</c> ;
    /// s'étire tout seul si un enfant s'étire (<c>Spacer()</c>, <c>.Grow()</c>, <c>.FillWidth()</c>).
    /// </summary>
    public static StackView VStack(params View?[] children) => Stack(Axis.Vertical, children);

    /// <summary>Aligne horizontalement ; mêmes règles que <see cref="VStack"/>.</summary>
    public static StackView HStack(params View?[] children) => Stack(Axis.Horizontal, children);

    /// <summary>Superpose les enfants (le dernier au-dessus), centrés sauf <c>.Alignment(…)</c>.</summary>
    public static ZStackView ZStack(params View?[] children) => new(Flatten(children));

    /// <summary>Plusieurs vues comme une seule : dans une pile, elles prennent place parmi ses enfants.</summary>
    public static GroupView Group(params View?[] children) => new(Flatten(children));

    /// <summary>Dessin libre. <code>Canvas(d => d.FillCircle(d.Bounds.Center, 8, ColorRole.Accent)).Size(16, 16)</code></summary>
    public static CanvasView Canvas(Action<DrawScope> draw) => new(draw);

    public static SpacerView Spacer() => new() { Grow = 1 };
    public static BoxView Box() => new();
    /// <summary>Trait horizontal ; épaisseur et couleur du thème (<c>DividerThickness</c>, <c>DividerColor</c>).</summary>
    public static DividerView Divider() => new() { Width = float.PositiveInfinity };

    /// <summary>
    /// Une vue par élément, chacune recalculée seule quand un état qu'elle lit change
    /// (ex. cocher une tâche ne recalcule que sa ligne). <paramref name="key"/> identifie l'élément
    /// (par défaut : l'objet lui-même) pour garder focus et survol quand la liste change.
    /// Se mélange aux autres enfants d'une pile : <c>VStack(Header(), ForEach(todos, Row, t => t.Id), Footer())</c>.
    /// </summary>
    public static GroupView ForEach<T>(IEnumerable<T> items, Func<T, View> row, Func<T, object>? key = null) =>
        new([.. items.Select(item => new ScopeView(() => row(item)) { Key = KeyOf(item, key) })]);

    /// <summary>Bloc recalculé seul quand les états qu'il lit changent, sans créer de composant.</summary>
    public static ScopeView Observe(Func<View> body) => new(body);

    // ── Menus ──

    /// <summary>
    /// Barre de menus : dessinée en haut de la fenêtre, ou celle du système, selon le réglage de l'hôte
    /// (<c>WindowOptions.MenuBar</c>). Se place dans le <c>Body</c> comme une vue : elle suit l'état.
    /// <code>MenuBar(Menu("&amp;Fichier", MenuItem("Nouveau", New).Shortcut("Ctrl+N")), Menu("&amp;Aide", …))</code>
    /// </summary>
    public static MenuBarView MenuBar(params View?[] menus) => new([.. MenuEntries(menus).OfType<MenuView>()]);

    /// <summary>Menu ou sous-menu : éléments, sous-menus, séparateurs (<c>Divider()</c>), listes (<c>ForEach</c>).</summary>
    public static MenuView Menu(string title, params View?[] items) => new(title, MenuEntries(items));

    public static MenuItemView MenuItem(string label, Action? action = null) => new(label, action);

    /// <summary>Élément à action asynchrone ; ses erreurs sont signalées à l'hôte, comme pour un bouton.</summary>
    public static MenuItemView MenuItem(string label, Func<Task> action) => new(label, Async(action));

    /// <summary>Élément coché lié à un état : <c>MenuItem("Mode sombre", Bind.DarkMode)</c> l'inverse à chaque clic.</summary>
    public static MenuItemView MenuItem(string label, Binding<bool> isChecked) =>
        new(label, () => isChecked.Set(!isChecked.Value)) { IsChecked = isChecked.Value };

    /// <summary>
    /// Éléments d'un menu : comme pour une pile, les null sont ignorés et les groupes dépliés ; les lignes de
    /// <c>ForEach</c> sont calculées tout de suite (un menu n'a pas de portées : il suit l'état de la vue qui le déclare).
    /// </summary>
    static View[] MenuEntries(IEnumerable<View?> items) =>
        [.. Flatten(items).SelectMany(v => v is ScopeView scope ? Flatten([scope.Build()]) : [v])];

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, string> RefKeys = new();
    static int _nextRefKey;

    static string? KeyOf<T>(T item, Func<T, object>? key)
    {
        if (key is not null) return key(item)?.ToString();
        if (item is null) return null;
        if (typeof(T).IsValueType || item is string) return item.ToString();
        return RefKeys.GetValue(item, _ => $"#{Interlocked.Increment(ref _nextRefKey)}");
    }

    static StackView Stack(Axis axis, View?[] children) =>
        new(axis, Flatten(children)) { CrossAlign = axis == Axis.Horizontal ? Align.Center : Align.Start };

    /// <summary>
    /// Enfants d'une pile : les null sont ignorés (permet <c>cond ? Text("…") : null</c>) et les groupes
    /// sans modificateur (<c>ForEach</c>, <c>Group</c>) sont dépliés à leur place.
    /// </summary>
    static View[] Flatten(IEnumerable<View?> children) => [.. children.SelectMany(Expand)];

    static IEnumerable<View> Expand(View? view) => view switch
    {
        null => [],
        GroupView group when group.HasNoModifiers() => group.Children.SelectMany(Expand),
        _ => [view],
    };
}
