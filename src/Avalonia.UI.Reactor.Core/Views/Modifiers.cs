// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Provides the chainable modifiers of views, in the style of SwiftUI.
/// </summary>
/// <remarks>
/// Each modifier returns a copy of the view, of the same type. The same modifiers exist for components in
/// <see cref="ComponentModifiers"/>.
/// </remarks>
public static class Modifiers
{
    /// <summary>
    /// Sets the stable identity of a view in a list, which keeps its control, focus and hover when the order changes.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="key">The identity of the view among its siblings.</param>
    /// <returns>A copy of the view with the key.</returns>
    public static T Key<T>(this T view, string key)
        where T : View => (T)(AsView(view) with { Key = key });

    /// <summary>
    /// Sets the same inner spacing on every side.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="all">The spacing of every side.</param>
    /// <returns>A copy of the view with the padding.</returns>
    public static T Padding<T>(this T view, float all)
        where T : View => (T)(AsView(view) with { Padding = new(all) });

    /// <summary>
    /// Sets a horizontal and a vertical inner spacing.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="horizontal">The spacing of the left and right sides.</param>
    /// <param name="vertical">The spacing of the top and bottom sides.</param>
    /// <returns>A copy of the view with the padding.</returns>
    public static T Padding<T>(this T view, float horizontal, float vertical)
        where T : View => (T)(AsView(view) with { Padding = new(horizontal, vertical) });

    /// <summary>
    /// Sets the inner spacing of each side.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="left">The spacing of the left side.</param>
    /// <param name="top">The spacing of the top side.</param>
    /// <param name="right">The spacing of the right side.</param>
    /// <param name="bottom">The spacing of the bottom side.</param>
    /// <returns>A copy of the view with the padding.</returns>
    public static T Padding<T>(this T view, float left, float top, float right, float bottom)
        where T : View => (T)(AsView(view) with { Padding = new(left, top, right, bottom) });

    /// <summary>
    /// Sets a fixed width.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="width">The width, in device-independent units.</param>
    /// <returns>A copy of the view with the width.</returns>
    public static T Width<T>(this T view, float width)
        where T : View => (T)(AsView(view) with { Width = width });

    /// <summary>
    /// Sets a fixed height.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="height">The height, in device-independent units.</param>
    /// <returns>A copy of the view with the height.</returns>
    public static T Height<T>(this T view, float height)
        where T : View => (T)(AsView(view) with { Height = height });

    /// <summary>
    /// Sets a fixed size.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="width">The width, in device-independent units.</param>
    /// <param name="height">The height, in device-independent units.</param>
    /// <returns>A copy of the view with the size.</returns>
    public static T Size<T>(this T view, float width, float height)
        where T : View => (T)(AsView(view) with { Width = width, Height = height });

    /// <summary>
    /// Makes the view fill the available width.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <returns>A copy of the view that fills the width.</returns>
    public static T FillWidth<T>(this T view)
        where T : View => (T)(AsView(view) with { Width = float.PositiveInfinity });

    /// <summary>
    /// Makes the view fill the available height.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <returns>A copy of the view that fills the height.</returns>
    public static T FillHeight<T>(this T view)
        where T : View => (T)(AsView(view) with { Height = float.PositiveInfinity });

    /// <summary>
    /// Makes the view take a share of the remaining space on the main axis of its stack.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="factor">The share of the remaining space, relative to the other growing views.</param>
    /// <returns>A copy of the view that grows.</returns>
    public static T Grow<T>(this T view, float factor = 1)
        where T : View => (T)(AsView(view) with { Grow = factor });

    /// <summary>
    /// Sets the alignment of the view on the cross axis of its stack.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="align">The alignment.</param>
    /// <returns>A copy of the view with the alignment.</returns>
    public static T AlignSelf<T>(this T view, Align align)
        where T : View => (T)(AsView(view) with { SelfAlign = align });

    /// <summary>
    /// Sets the background color.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="color">A fixed color, a role or a named color of the theme.</param>
    /// <returns>A copy of the view with the background.</returns>
    public static T Background<T>(this T view, ColorValue color)
        where T : View => (T)(AsView(view) with { Background = color });

    /// <summary>
    /// Sets a border; the border takes its own space, as in CSS.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="color">The border color.</param>
    /// <param name="width">The border thickness.</param>
    /// <returns>A copy of the view with the border.</returns>
    public static T Border<T>(this T view, ColorValue color, float width = 1)
        where T : View => (T)(AsView(view) with { BorderColor = color, BorderWidth = width });

    /// <summary>
    /// Sets the corner radius.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="radius">The corner radius.</param>
    /// <returns>A copy of the view with the corner radius.</returns>
    public static T Radius<T>(this T view, float radius)
        where T : View => (T)(AsView(view) with { CornerRadius = radius });

    /// <summary>
    /// Sets the text color of the view and its descendants.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="color">A fixed color, a role or a named color of the theme.</param>
    /// <returns>A copy of the view with the text color.</returns>
    public static T Foreground<T>(this T view, ColorValue color)
        where T : View => (T)(AsView(view) with { Foreground = color });

    /// <summary>
    /// Sets the font size of the view and its descendants.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="size">The font size, in device-independent units.</param>
    /// <returns>A copy of the view with the font size.</returns>
    public static T FontSize<T>(this T view, float size)
        where T : View => (T)(AsView(view) with { FontSize = size });

    /// <summary>
    /// Makes the text of the view and its descendants bold, with the bold weight of the theme.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="bold"><see langword="true"/> for bold text; <see langword="false"/> for regular text.</param>
    /// <returns>A copy of the view with the weight.</returns>
    public static T Bold<T>(this T view, bool bold = true)
        where T : View => (T)(AsView(view) with { Bold = bold });

    /// <summary>
    /// Sets a built-in text style of the theme.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="style">The text style.</param>
    /// <returns>A copy of the view with the text style.</returns>
    public static T Font<T>(this T view, TextStyle style)
        where T : View => (T)(AsView(view) with { TextStyle = style });

    /// <summary>
    /// Sets a text style of the theme by name, including the styles added with
    /// <see cref="Theming.Theme.WithTextStyle"/>.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="styleName">The name of the text style, case-insensitive.</param>
    /// <returns>A copy of the view with the text style.</returns>
    public static T Font<T>(this T view, string styleName)
        where T : View => (T)(AsView(view) with { TextStyleName = styleName });

    /// <summary>
    /// Dresses the view and its descendants with a family of the catalog, by name: <c>Button("OK").Theme("Sable")</c>.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="name">The name of a family (or a theme) of the catalog, light or dark depending on the mode;
    /// <see langword="null"/> for the theme of the parent.</param>
    /// <returns>A copy of the view with the theme.</returns>
    public static T Theme<T>(this T view, string? name)
        where T : View => (T)(AsView(view) with { ThemeName = name });

    /// <summary>
    /// Dresses the view and its descendants with a theme family, light or dark depending on the mode.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="family">The theme family.</param>
    /// <returns>A copy of the view with the theme.</returns>
    public static T Theme<T>(this T view, ThemeFamily family)
        where T : View => (T)(AsView(view) with { ThemeFamily = family });

    /// <summary>
    /// Dresses the view and its descendants with an exact theme, whose variant does not change with the mode.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="theme">The theme.</param>
    /// <returns>A copy of the view with the theme.</returns>
    public static T Theme<T>(this T view, Theme theme)
        where T : View => (T)(AsView(view) with { Theme = theme });

    /// <summary>
    /// Forces the light or dark variant of the current family on the view and its descendants.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="scheme">The color scheme; <see langword="null"/> to follow the parent, or the system.</param>
    /// <returns>A copy of the view with the color scheme.</returns>
    public static T ColorScheme<T>(this T view, ColorScheme? scheme)
        where T : View => (T)(AsView(view) with { Scheme = scheme });

    /// <summary>
    /// Makes the view clickable, with the hover and pressed overlays of the theme.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="action">The action run when the view is clicked.</param>
    /// <returns>A copy of the view that responds to clicks.</returns>
    public static T OnTap<T>(this T view, Action action)
        where T : View => (T)(AsView(view) with { TapAction = action });

    /// <summary>
    /// Makes the view clickable with an asynchronous action, whose errors are reported by the host.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view to modify.</param>
    /// <param name="action">The asynchronous action run when the view is clicked.</param>
    /// <returns>A copy of the view that responds to clicks.</returns>
    public static T OnTap<T>(this T view, Func<Task> action)
        where T : View => (T)(AsView(view) with { TapAction = Ui.Async(action) });

    /// <summary>
    /// Places a view over this one (a badge, an overlay), aligned within its bounds.
    /// </summary>
    /// <param name="view">The view below.</param>
    /// <param name="overlay">The view on top.</param>
    /// <param name="alignment">The position of the overlay; centered by default.</param>
    /// <returns>The layered view.</returns>
    public static ZStackView Overlay(this View view, View overlay, Alignment? alignment = null) =>
        Ui.ZStack(view, overlay) with { Alignment = alignment ?? Primitives.Alignment.Center };

    /// <summary>
    /// Sets the space between the children of a stack.
    /// </summary>
    /// <param name="stack">The stack to modify.</param>
    /// <param name="spacing">The space between the children.</param>
    /// <returns>A copy of the stack with the spacing.</returns>
    public static StackView Spacing(this StackView stack, float spacing) => stack with { Spacing = spacing };

    /// <summary>
    /// Sets the alignment of the children of a stack on its cross axis.
    /// </summary>
    /// <param name="stack">The stack to modify.</param>
    /// <param name="align">The alignment of the children.</param>
    /// <returns>A copy of the stack with the alignment.</returns>
    public static StackView Alignment(this StackView stack, Align align) => stack with { CrossAlign = align };

    /// <summary>
    /// Sets the position of the layers that are smaller than the stack.
    /// </summary>
    /// <param name="stack">The layered view to modify.</param>
    /// <param name="alignment">The position of the layers.</param>
    /// <returns>A copy of the layered view with the alignment.</returns>
    public static ZStackView Alignment(this ZStackView stack, Alignment alignment) => stack with { Alignment = alignment };

    /// <summary>
    /// Sets the horizontal alignment of a text in its place.
    /// </summary>
    /// <param name="text">The text to modify.</param>
    /// <param name="align">The alignment of the text.</param>
    /// <returns>A copy of the text with the alignment.</returns>
    public static TextView TextAlignment(this TextView text, TextAlign align) => text with { TextAlign = align };

    /// <summary>
    /// Sets a built-in style on a button.
    /// </summary>
    /// <param name="button">The button to modify.</param>
    /// <param name="style">The style.</param>
    /// <returns>A copy of the button with the style.</returns>
    public static ButtonView Style(this ButtonView button, ButtonStyle style) => button with { Style = style };

    /// <summary>
    /// Sets custom colors on a button.
    /// </summary>
    /// <param name="button">The button to modify.</param>
    /// <param name="variant">The colors of the button.</param>
    /// <returns>A copy of the button with the colors.</returns>
    public static ButtonView Style(this ButtonView button, ButtonVariant variant) => button with { Variant = variant };

    /// <summary>
    /// Sets a button style of the theme by name, including the styles added with
    /// <see cref="Theming.Theme.WithButtonStyle"/>.
    /// </summary>
    /// <param name="button">The button to modify.</param>
    /// <param name="styleName">The name of the style, case-insensitive.</param>
    /// <returns>A copy of the button with the style.</returns>
    public static ButtonView Style(this ButtonView button, string styleName) => button with { StyleName = styleName };

    /// <summary>
    /// Replaces the look of this button only; for every button, use the template of the theme.
    /// </summary>
    /// <param name="button">The button to modify.</param>
    /// <param name="template">The function that builds the look of the button from its state.</param>
    /// <returns>A copy of the button with the template.</returns>
    public static ButtonView Template(this ButtonView button, Func<ButtonState, View> template) =>
        button with { CustomTemplate = template };

    /// <summary>
    /// Replaces the look of this checkbox only; for every checkbox, use the template of the theme.
    /// </summary>
    /// <param name="checkbox">The checkbox to modify.</param>
    /// <param name="template">The function that builds the look of the checkbox from its state.</param>
    /// <returns>A copy of the checkbox with the template.</returns>
    public static CheckboxView Template(this CheckboxView checkbox, Func<CheckboxState, View> template) =>
        checkbox with { CustomTemplate = template };

    /// <summary>
    /// Replaces the look of this text field only; for every text field, use the template of the theme.
    /// </summary>
    /// <param name="field">The text field to modify.</param>
    /// <param name="template">The function that builds the look of the field from its state.</param>
    /// <returns>A copy of the text field with the template.</returns>
    public static TextFieldView Template(this TextFieldView field, Func<TextFieldState, View> template) =>
        field with { CustomTemplate = template };

    /// <summary>
    /// Sets the keyboard shortcut of a menu item, active even when the menus are closed: <c>.Shortcut("Ctrl+N")</c>
    /// (Cmd+N on macOS).
    /// </summary>
    /// <param name="item">The menu item to modify.</param>
    /// <param name="gesture">The shortcut, as read by <see cref="KeyGesture.Parse"/>.</param>
    /// <returns>A copy of the menu item with the shortcut.</returns>
    /// <exception cref="ArgumentException"><paramref name="gesture"/> is not a valid shortcut.</exception>
    public static MenuItemView Shortcut(this MenuItemView item, string gesture) => item with { Gesture = KeyGesture.Parse(gesture) };

    /// <summary>
    /// Sets the keyboard shortcut of a menu item, active even when the menus are closed.
    /// </summary>
    /// <param name="item">The menu item to modify.</param>
    /// <param name="gesture">The shortcut.</param>
    /// <returns>A copy of the menu item with the shortcut.</returns>
    public static MenuItemView Shortcut(this MenuItemView item, KeyGesture gesture) => item with { Gesture = gesture };

    /// <summary>
    /// Enables or disables a menu item; a disabled item is dimmed, and neither its click nor its shortcut runs the action.
    /// </summary>
    /// <param name="item">The menu item to modify.</param>
    /// <param name="enabled"><see langword="true"/> to enable the item; <see langword="false"/> to disable it.</param>
    /// <returns>A copy of the menu item.</returns>
    public static MenuItemView Enabled(this MenuItemView item, bool enabled = true) => item with { IsEnabled = enabled };

    /// <summary>
    /// Shows or hides a check mark before the label of a menu item, for example on the chosen item of exclusive choices.
    /// </summary>
    /// <param name="item">The menu item to modify.</param>
    /// <param name="isChecked"><see langword="true"/> to show the check mark.</param>
    /// <returns>A copy of the menu item.</returns>
    public static MenuItemView Checked(this MenuItemView item, bool isChecked = true) => item with { IsChecked = isChecked };

    // A "with" expression needs a record type: a type parameter is not one, so views are copied as View.
    private static View AsView(View view) => view;
}
