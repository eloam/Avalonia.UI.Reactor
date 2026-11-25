// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using MartletUI.Primitives;
using MartletUI.State;
using MartletUI.Theming;

namespace MartletUI.Views;

/// <summary>
/// Provides the modifiers of <see cref="Modifiers"/> for components: <c>counter.Padding(8).Background(ColorRole.Surface)</c>.
/// </summary>
/// <remarks>
/// The modifiers apply around the content of the component, which keeps its state and its own re-evaluation. They are
/// needed because extension methods on <see cref="View"/> do not apply to a component, which only converts to a view.
/// </remarks>
public static class ComponentModifiers
{
    /// <summary>
    /// Sets the stable identity of the component in a list.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="key">The identity of the component among its siblings.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Key(this Component component, string key) => AsView(component).Key(key);

    /// <summary>
    /// Sets the same inner spacing on every side.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="all">The spacing of every side.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Padding(this Component component, float all) => AsView(component).Padding(all);

    /// <summary>
    /// Sets a horizontal and a vertical inner spacing.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="horizontal">The spacing of the left and right sides.</param>
    /// <param name="vertical">The spacing of the top and bottom sides.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Padding(this Component component, float horizontal, float vertical) => AsView(component).Padding(horizontal, vertical);

    /// <summary>
    /// Sets the inner spacing of each side.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="left">The spacing of the left side.</param>
    /// <param name="top">The spacing of the top side.</param>
    /// <param name="right">The spacing of the right side.</param>
    /// <param name="bottom">The spacing of the bottom side.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Padding(this Component component, float left, float top, float right, float bottom) => AsView(component).Padding(left, top, right, bottom);

    /// <summary>
    /// Sets a fixed width.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="width">The width, in device-independent units.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Width(this Component component, float width) => AsView(component).Width(width);

    /// <summary>
    /// Sets a fixed height.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="height">The height, in device-independent units.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Height(this Component component, float height) => AsView(component).Height(height);

    /// <summary>
    /// Sets a fixed size.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="width">The width, in device-independent units.</param>
    /// <param name="height">The height, in device-independent units.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Size(this Component component, float width, float height) => AsView(component).Size(width, height);

    /// <summary>
    /// Makes the component fill the available width.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View FillWidth(this Component component) => AsView(component).FillWidth();

    /// <summary>
    /// Makes the component fill the available height.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View FillHeight(this Component component) => AsView(component).FillHeight();

    /// <summary>
    /// Makes the component take a share of the remaining space on the main axis of its stack.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="factor">The share of the remaining space, relative to the other growing views.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Grow(this Component component, float factor = 1) => AsView(component).Grow(factor);

    /// <summary>
    /// Sets the alignment of the component on the cross axis of its stack.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="align">The alignment.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View AlignSelf(this Component component, Align align) => AsView(component).AlignSelf(align);

    /// <summary>
    /// Sets the background color.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="color">A fixed color, a role or a named color of the theme.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Background(this Component component, ColorValue color) => AsView(component).Background(color);

    /// <summary>
    /// Sets a border; the border takes its own space, as in CSS.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="color">The border color.</param>
    /// <param name="width">The border thickness.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Border(this Component component, ColorValue color, float width = 1) => AsView(component).Border(color, width);

    /// <summary>
    /// Sets the corner radius.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="radius">The corner radius.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Radius(this Component component, float radius) => AsView(component).Radius(radius);

    /// <summary>
    /// Sets the text color of the component.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="color">A fixed color, a role or a named color of the theme.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Foreground(this Component component, ColorValue color) => AsView(component).Foreground(color);

    /// <summary>
    /// Sets the font size of the component.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="size">The font size, in device-independent units.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View FontSize(this Component component, float size) => AsView(component).FontSize(size);

    /// <summary>
    /// Makes the text of the component bold, with the bold weight of the theme.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="bold"><see langword="true"/> for bold text; <see langword="false"/> for regular text.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Bold(this Component component, bool bold = true) => AsView(component).Bold(bold);

    /// <summary>
    /// Sets a built-in text style of the theme.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="style">The text style.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Font(this Component component, TextStyle style) => AsView(component).Font(style);

    /// <summary>
    /// Sets a text style of the theme by name.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="styleName">The name of the text style, case-insensitive.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Font(this Component component, string styleName) => AsView(component).Font(styleName);

    /// <summary>
    /// Dresses the component with a family of the catalog, by name.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="name">The name of a family (or a theme) of the catalog; <see langword="null"/> for the theme of the parent.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Theme(this Component component, string? name) => AsView(component).Theme(name);

    /// <summary>
    /// Dresses the component with a theme family, light or dark depending on the mode.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="family">The theme family.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Theme(this Component component, ThemeFamily family) => AsView(component).Theme(family);

    /// <summary>
    /// Dresses the component with an exact theme.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="theme">The theme.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Theme(this Component component, Theme theme) => AsView(component).Theme(theme);

    /// <summary>
    /// Forces the light or dark variant of the current family on the component.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="scheme">The color scheme; <see langword="null"/> to follow the parent, or the system.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View ColorScheme(this Component component, ColorScheme? scheme) => AsView(component).ColorScheme(scheme);

    /// <summary>
    /// Makes the component clickable, with the hover and pressed overlays of the theme.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="action">The action run when the component is clicked.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View OnTap(this Component component, Action action) => AsView(component).OnTap(action);

    /// <summary>
    /// Makes the component clickable with an asynchronous action, whose errors are reported by the host.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="action">The asynchronous action run when the component is clicked.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View OnTap(this Component component, Func<Task> action) => AsView(component).OnTap(action);

    /// <summary>
    /// Runs an action once when the component appears.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="action">The action run when the component appears.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View OnAppear(this Component component, Action action) => AsView(component).OnAppear(action);

    /// <summary>
    /// Runs an action once when the component disappears.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="action">The action run when the component disappears.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View OnDisappear(this Component component, Action action) => AsView(component).OnDisappear(action);

    /// <summary>
    /// Runs a task while the component is displayed; the task is cancelled when it disappears.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="action">The task, which receives the token cancelled when the component disappears.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Task(this Component component, Func<CancellationToken, Task> action) =>
        AsView(component).Task(action);

    /// <summary>
    /// Runs a task while the component is displayed, and starts it again when its key changes.
    /// </summary>
    /// <param name="component">The component to modify.</param>
    /// <param name="key">The value whose change starts the task again.</param>
    /// <param name="action">The task, which receives the token cancelled when it must stop.</param>
    /// <returns>The view of the component with the modifier.</returns>
    public static View Task(this Component component, object? key, Func<CancellationToken, Task> action) =>
        AsView(component).Task(key, action);

    /// <summary>
    /// Places a view over the component (a badge, an overlay), aligned within its bounds.
    /// </summary>
    /// <param name="component">The component below.</param>
    /// <param name="overlay">The view on top.</param>
    /// <param name="alignment">The position of the overlay; centered by default.</param>
    /// <returns>The layered view.</returns>
    public static ZStackView Overlay(this Component component, View overlay, Alignment? alignment = null) =>
        AsView(component).Overlay(overlay, alignment);

    private static View AsView(Component component) => component;
}
