// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Rendering;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.State;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Provides the factories of views: the domain-specific language of the framework.
/// </summary>
/// <remarks>
/// Import it with <c>using static Avalonia.UI.Reactor.Views.Ui;</c>, and views need no <see langword="new"/>. Factories
/// set no style: everything comes from the theme and can be overridden by modifiers. Children always come first;
/// spacing, alignment and other settings are modifiers.
/// </remarks>
public static class Ui
{
    /// <summary>
    /// Creates a single line of text.
    /// </summary>
    /// <param name="content">The text to display.</param>
    /// <returns>The text view.</returns>
    public static TextView Text(string content) => new(content);

    /// <summary>
    /// Creates a push button.
    /// </summary>
    /// <param name="label">The text of the button.</param>
    /// <param name="action">The action run when the button is clicked.</param>
    /// <returns>The button view.</returns>
    public static ButtonView Button(string label, Action? action = null) => new(label, action);

    /// <summary>
    /// Creates a push button that runs an asynchronous action: <c>Button("Load", LoadAsync)</c>.
    /// </summary>
    /// <remarks>
    /// The interface updates before and after each <see langword="await"/>. An exception is reported by the host,
    /// prefixed with <c>[Reactor]</c>, instead of closing the application.
    /// </remarks>
    /// <param name="label">The text of the button.</param>
    /// <param name="action">The asynchronous action run when the button is clicked.</param>
    /// <returns>The button view.</returns>
    public static ButtonView Button(string label, Func<Task> action) => new(label, Async(action));

    /// <summary>
    /// Creates a text field bound to state: <c>TextField(Bind.Draft, "Name")</c> reads and writes <c>Draft</c>.
    /// </summary>
    /// <param name="text">The binding to the text.</param>
    /// <param name="placeholder">The text shown while the field is empty.</param>
    /// <param name="onSubmit">The action run when the user presses Enter.</param>
    /// <returns>The text field view.</returns>
    public static TextFieldView TextField(Binding<string> text, string placeholder = "", Action? onSubmit = null) =>
        TextField(text.Value, text.Set, placeholder, onSubmit) with { BindingTarget = text.Target };

    /// <summary>
    /// Creates a text field with a computed value: the value, then what to do when the user changes it.
    /// </summary>
    /// <param name="value">The text shown by the field.</param>
    /// <param name="onChange">The action that receives the text typed by the user.</param>
    /// <param name="placeholder">The text shown while the field is empty.</param>
    /// <param name="onSubmit">The action run when the user presses Enter.</param>
    /// <returns>The text field view.</returns>
    public static TextFieldView TextField(string value, Action<string>? onChange, string placeholder = "", Action? onSubmit = null) =>
        new(value, onChange) { Placeholder = placeholder, OnSubmit = onSubmit };

    /// <summary>
    /// Creates a checkbox bound to state: <c>Checkbox(todo.Bind.Done)</c>.
    /// </summary>
    /// <param name="isChecked">The binding to the checked state.</param>
    /// <param name="label">The label of the checkbox.</param>
    /// <returns>The checkbox view.</returns>
    public static CheckboxView Checkbox(Binding<bool> isChecked, string? label = null) =>
        Checkbox(isChecked.Value, isChecked.Set, label) with { BindingTarget = isChecked.Target };

    /// <summary>
    /// Creates a checkbox with a computed value: the value, then what to do when the user toggles it.
    /// </summary>
    /// <param name="isChecked">A value indicating whether the box is checked.</param>
    /// <param name="onChange">The action that receives the new value.</param>
    /// <param name="label">The label of the checkbox.</param>
    /// <returns>The checkbox view.</returns>
    public static CheckboxView Checkbox(bool isChecked, Action<bool>? onChange = null, string? label = null) =>
        new(isChecked, onChange, label);

    /// <summary>
    /// Creates a vertical stack.
    /// </summary>
    /// <remarks>
    /// The children are spaced by the theme (<see cref="Theming.Theme.Spacing"/>) unless <c>.Spacing(…)</c> is set. The
    /// stack stretches on its own when a child stretches (<c>Spacer()</c>, <c>.Grow()</c>, <c>.FillWidth()</c>).
    /// <see langword="null"/> children are ignored and groups are unfolded in place.
    /// </remarks>
    /// <param name="children">The views of the stack, from top to bottom.</param>
    /// <returns>The stack view.</returns>
    public static StackView VStack(params View?[] children) => Stack(Axis.Vertical, children);

    /// <summary>
    /// Creates a vertical stack whose children are the views of a block, as in SwiftUI.
    /// </summary>
    /// <remarks>
    /// In the block, a statement that creates a view adds it to the stack, and <see langword="if"/>,
    /// <see langword="else"/>, <see langword="switch"/> and local variables decide which views are added. For a fixed list
    /// of views, pass them as arguments instead: <c>VStack(a, b)</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// VStack(() =&gt;
    /// {
    ///     Header();
    ///     if (IsLoggedIn)
    ///         Text($"Hello, {Name}!");
    ///     else
    ///         Button("Sign in", SignIn);
    /// })
    /// </code>
    /// </example>
    /// <param name="content">The block, whose views are the children of the stack, from top to bottom.</param>
    /// <returns>The stack view.</returns>
    public static StackView VStack([ViewBuilder] Action content) => Stack(Axis.Vertical, ViewCollector.Collect(content, content));

    /// <summary>
    /// Creates a horizontal stack, with the same rules as <see cref="VStack(View[])"/>; children are centered vertically.
    /// </summary>
    /// <param name="children">The views of the stack, from left to right.</param>
    /// <returns>The stack view.</returns>
    public static StackView HStack(params View?[] children) => Stack(Axis.Horizontal, children);

    /// <summary>
    /// Creates a horizontal stack whose children are the views of a block (see <see cref="VStack(Action)"/>).
    /// </summary>
    /// <param name="content">The block, whose views are the children of the stack, from left to right.</param>
    /// <returns>The stack view.</returns>
    public static StackView HStack([ViewBuilder] Action content) => Stack(Axis.Horizontal, ViewCollector.Collect(content, content));

    /// <summary>
    /// Creates layers, the last child on top, centered unless <c>.Alignment(…)</c> is set.
    /// </summary>
    /// <param name="children">The layers, from bottom to top.</param>
    /// <returns>The layered view.</returns>
    public static ZStackView ZStack(params View?[] children) => new(Flatten(children));

    /// <summary>
    /// Creates layers whose views are those of a block, the last one on top (see <see cref="VStack(Action)"/>).
    /// </summary>
    /// <param name="content">The block, whose views are the layers, from bottom to top.</param>
    /// <returns>The layered view.</returns>
    public static ZStackView ZStack([ViewBuilder] Action content) => new(Flatten(ViewCollector.Collect(content, content)));

    /// <summary>
    /// Creates several views presented as one; in a stack, they take their places among the children of the stack.
    /// </summary>
    /// <remarks>
    /// As in SwiftUI, a modifier of the group applies to each child that does not set it itself:
    /// <c>HStack(Group(a, b).Padding(8))</c> pads <c>a</c> and <c>b</c>, side by side. The key of a group identifies it:
    /// it is placed above the keys of its children.
    /// </remarks>
    /// <param name="children">The views of the group.</param>
    /// <returns>The group view.</returns>
    public static GroupView Group(params View?[] children) => new(Flatten(children));

    /// <summary>
    /// Creates several views presented as one, from a block: the reusable piece of a view, as
    /// <c>@ViewBuilder var details: some View</c> in SwiftUI (see <see cref="VStack(Action)"/>).
    /// </summary>
    /// <example>
    /// <code>
    /// private View Details() =&gt; Group(() =&gt;
    /// {
    ///     Text("Size: 42");
    ///     if (HasColor)
    ///         Text("Color: blue");
    /// });
    /// </code>
    /// </example>
    /// <param name="content">The block, whose views are those of the group.</param>
    /// <returns>The group view.</returns>
    public static GroupView Group([ViewBuilder] Action content) => new(Flatten(ViewCollector.Collect(content, content)));

    /// <summary>
    /// Adds a view to the view builder block being run: a component, a variable, a conditional or a
    /// <see langword="switch"/> expression, which C# does not accept as statements.
    /// </summary>
    /// <remarks>
    /// A call that creates a view (<c>Text("a");</c>) is already a statement of the block: <c>Show</c> is for the other
    /// expressions. A <see langword="null"/> view adds nothing, as a <see langword="null"/> child does.
    /// </remarks>
    /// <example>
    /// <code>
    /// VStack(() =&gt;
    /// {
    ///     Show(_counter);
    ///     Show(Tab switch
    ///     {
    ///         Tab.Home =&gt; Home(),
    ///         Tab.Settings =&gt; Settings(),
    ///     });
    /// })
    /// </code>
    /// </example>
    /// <param name="view">The view to add, or <see langword="null"/> to add nothing.</param>
    /// <exception cref="InvalidOperationException">The call is not in a view builder block.</exception>
    public static void Show(View? view) => ViewCollector.Show(view);

    /// <summary>
    /// Creates a free drawing: <c>Canvas(d =&gt; d.FillCircle(d.Bounds.Center, 8, ColorRole.Accent)).Size(16, 16)</c>.
    /// </summary>
    /// <param name="draw">The function that draws the canvas; it is called again when the state it reads changes.</param>
    /// <returns>The canvas view.</returns>
    public static CanvasView Canvas(Action<DrawScope> draw) => new(draw);

    /// <summary>
    /// Creates flexible space that pushes the next views of its stack to the end.
    /// </summary>
    /// <returns>The spacer view.</returns>
    public static SpacerView Spacer() => new() { Grow = 1 };

    /// <summary>
    /// Creates an empty surface, shaped by its modifiers: <c>Box().Size(40, 40).Background(ColorRole.Accent)</c>.
    /// </summary>
    /// <returns>The box view.</returns>
    public static BoxView Box() => new();

    /// <summary>
    /// Creates a horizontal line with the thickness and color of the theme
    /// (<see cref="Theming.Theme.DividerThickness"/>, <see cref="Theming.Theme.DividerColor"/>); in a menu, a separator.
    /// </summary>
    /// <returns>The divider view.</returns>
    public static DividerView Divider() => new() { Width = float.PositiveInfinity };

    /// <summary>
    /// Creates one view per item, each re-evaluated on its own when the state it reads changes.
    /// </summary>
    /// <remarks>
    /// Checking a task, for example, re-evaluates its row only. The result mixes with the other children of a stack:
    /// <c>VStack(Header(), ForEach(todos, Row, t =&gt; t.Id), Footer())</c>.
    /// </remarks>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="items">The items.</param>
    /// <param name="row">The function that builds the view of an item.</param>
    /// <param name="key">The function that identifies an item, to keep focus and hover when the list changes; by
    /// default, the <see cref="IIdentifiable{TId}.Id"/> of an item that implements <see cref="IIdentifiable{TId}"/>,
    /// otherwise the item itself.</param>
    /// <returns>The group of item views.</returns>
    public static GroupView ForEach<T>(IEnumerable<T> items, Func<T, View> row, Func<T, object>? key = null) =>
        new([.. items.Select(item => new ScopeView(() => row(item)) { Key = KeyOf(item, key) })]);

    /// <summary>
    /// Creates the views of each item from a block, each row re-evaluated on its own, as <c>ForEach</c> in SwiftUI.
    /// </summary>
    /// <remarks>
    /// The block of a row runs in the scope of the row: checking a task re-evaluates its row only. Its views take their
    /// places among the children of the container (see <see cref="VStack(Action)"/>).
    /// </remarks>
    /// <example>
    /// <code>
    /// ForEach(Todos, todo =&gt;
    /// {
    ///     Row(todo);
    ///     if (todo.IsLate)
    ///         Text("Late").Foreground(ColorRole.Danger);
    /// })
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="items">The items.</param>
    /// <param name="row">The block of a row, which receives its item.</param>
    /// <param name="key">The function that identifies an item (see <see cref="ForEach{T}(IEnumerable{T}, Func{T, View}, Func{T, object})"/>).</param>
    /// <returns>The group of item views.</returns>
    public static GroupView ForEach<T>(IEnumerable<T> items, [ViewBuilder] Action<T> row, Func<T, object>? key = null) =>
        new([.. items.Select(item =>
            new ScopeView(() => new GroupView(Flatten(ViewCollector.Collect(row, () => row(item))))) { Key = KeyOf(item, key) })]);

    /// <summary>
    /// Creates a block re-evaluated on its own when the state it reads changes, without creating a component.
    /// </summary>
    /// <param name="body">The function that builds the block.</param>
    /// <returns>The scope view.</returns>
    public static ScopeView Observe(Func<View> body) => new(body);

    /// <summary>
    /// Creates a block re-evaluated on its own when the state it reads changes, whose views are those of a view builder
    /// block (see <see cref="VStack(Action)"/>).
    /// </summary>
    /// <remarks>
    /// Among the children of a container, the views of the block take their places among the other children.
    /// </remarks>
    /// <example>
    /// <code>
    /// Observe(() =&gt;
    /// {
    ///     Text($"{Done}/{Total}");
    ///     if (Done == Total)
    ///         Text("All done");
    /// })
    /// </code>
    /// </example>
    /// <param name="content">The block, whose views are those of the scope.</param>
    /// <returns>The scope view.</returns>
    public static ScopeView Observe([ViewBuilder] Action content) =>
        new(() => new GroupView(Flatten(ViewCollector.Collect(content, content))));

    /// <summary>
    /// Creates the menu bar of the window: <c>MenuBar(Menu("&amp;File", MenuItem("New", New).Shortcut("Ctrl+N")))</c>.
    /// </summary>
    /// <remarks>
    /// The bar is placed in the <c>Body</c> like a view and follows the state. It is drawn at the top of the window, or
    /// handed to the system menu bar when the platform has one (macOS).
    /// </remarks>
    /// <param name="menus">The menus of the bar.</param>
    /// <returns>The menu bar view.</returns>
    public static MenuBarView MenuBar(params View?[] menus) => new([.. MenuEntries(menus).OfType<MenuView>()]);

    /// <summary>
    /// Creates a menu or a submenu.
    /// </summary>
    /// <param name="title">The title; the letter after <c>&amp;</c> is the access key.</param>
    /// <param name="items">The items, submenus, separators (<c>Divider()</c>) and lists (<c>ForEach</c>).</param>
    /// <returns>The menu view.</returns>
    public static MenuView Menu(string title, params View?[] items) => new(title, MenuEntries(items));

    /// <summary>
    /// Creates a menu item.
    /// </summary>
    /// <param name="label">The label; the letter after <c>&amp;</c> is the access key.</param>
    /// <param name="action">The action of the item.</param>
    /// <returns>The menu item view.</returns>
    public static MenuItemView MenuItem(string label, Action? action = null) => new(label, action);

    /// <summary>
    /// Creates a menu item that runs an asynchronous action; its errors are reported by the host, as for a button.
    /// </summary>
    /// <param name="label">The label; the letter after <c>&amp;</c> is the access key.</param>
    /// <param name="action">The asynchronous action of the item.</param>
    /// <returns>The menu item view.</returns>
    public static MenuItemView MenuItem(string label, Func<Task> action) => new(label, Async(action));

    /// <summary>
    /// Creates a checked menu item bound to state: <c>MenuItem("Dark mode", Bind.DarkMode)</c> toggles it on each click.
    /// </summary>
    /// <param name="label">The label; the letter after <c>&amp;</c> is the access key.</param>
    /// <param name="isChecked">The binding to the checked state.</param>
    /// <returns>The menu item view.</returns>
    public static MenuItemView MenuItem(string label, Binding<bool> isChecked) =>
        new(label, () => isChecked.Set(!isChecked.Value)) { IsChecked = isChecked.Value };

    /// <summary>
    /// Wraps a task started by an action of the user interface, so that its errors are reported to the host that started
    /// it, captured before the first <see langword="await"/>, without closing the application.
    /// </summary>
    /// <remarks>
    /// Outside an action, there is no host to report to and an error propagates normally.
    /// </remarks>
    /// <param name="action">The asynchronous action.</param>
    /// <returns>The synchronous action that starts it.</returns>
    internal static Action Async(Func<Task> action) => async () =>
    {
        IScopeHost? host = RunningAction.Host;
        try
        {
            await action();
        }
        catch (Exception exception) when (host is not null)
        {
            host.Report(exception);
        }
    };

    /// <summary>
    /// Returns the children of a group, ready to take their places among the children of a container.
    /// </summary>
    /// <remarks>
    /// Nested groups are unfolded too. As in SwiftUI, each modifier of a group applies to each of its children that does
    /// not set it itself; the key of the group is placed above the keys of its children.
    /// </remarks>
    /// <param name="group">The group.</param>
    /// <returns>The children of the group, with its modifiers.</returns>
    internal static IEnumerable<View> Unfold(GroupView group)
    {
        IEnumerable<View> children = group.Children.SelectMany(Expand);
        return group.HasNoModifiers() ? children : children.Select((child, position) => child.Inherit(group, position));
    }

    // The entries of a menu: as for a stack, null entries are ignored and groups are unfolded. ForEach rows are built
    // immediately: a menu has no scope of its own and follows the state of the view that declares it.
    private static View[] MenuEntries(IEnumerable<View?> items) =>
        [.. Flatten(items).SelectMany(v => v is ScopeView scope ? Flatten([scope.Build()]) : [v])];

    private static string? KeyOf<T>(T item, Func<T, object>? key)
    {
        if (key is not null)
        {
            return key(item)?.ToString();
        }

        if (item is null)
        {
            return null;
        }

        if (item is IIdentifiable identifiable)
        {
            return identifiable.Key.ToString();
        }

        if (typeof(T).IsValueType || item is string)
        {
            return item.ToString();
        }

        return ReferenceKeys.Of(item);
    }

    private static StackView Stack(Axis axis, View?[] children) =>
        new(axis, Flatten(children)) { CrossAlign = axis == Axis.Horizontal ? Align.Center : Align.Start };

    // The children of a stack: null children are ignored (for "condition ? Text(…) : null") and groups (ForEach, Group)
    // are unfolded in place (see Unfold).
    private static View[] Flatten(IEnumerable<View?> children) => [.. children.SelectMany(Expand)];

    private static IEnumerable<View> Expand(View? view) => view switch
    {
        null => [],
        GroupView group => Unfold(group),
        _ => [view],
    };
}
