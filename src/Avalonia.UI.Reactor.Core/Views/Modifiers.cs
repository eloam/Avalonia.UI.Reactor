using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Modificateurs chaînables façon SwiftUI. Chacun renvoie une copie (`with`) du même type.
/// </summary>
public static class Modifiers
{
    static View V(View v) => v;

    public static T Key<T>(this T v, string key) where T : View => (T)(V(v) with { Key = key });

    public static T Padding<T>(this T v, float all) where T : View => (T)(V(v) with { Padding = new(all) });
    public static T Padding<T>(this T v, float horizontal, float vertical) where T : View =>
        (T)(V(v) with { Padding = new(horizontal, vertical) });
    public static T Padding<T>(this T v, float left, float top, float right, float bottom) where T : View =>
        (T)(V(v) with { Padding = new(left, top, right, bottom) });

    public static T Width<T>(this T v, float width) where T : View => (T)(V(v) with { Width = width });
    public static T Height<T>(this T v, float height) where T : View => (T)(V(v) with { Height = height });
    public static T Size<T>(this T v, float width, float height) where T : View =>
        (T)(V(v) with { Width = width, Height = height });
    public static T FillWidth<T>(this T v) where T : View => (T)(V(v) with { Width = float.PositiveInfinity });
    public static T FillHeight<T>(this T v) where T : View => (T)(V(v) with { Height = float.PositiveInfinity });
    public static T Grow<T>(this T v, float factor = 1) where T : View => (T)(V(v) with { Grow = factor });
    public static T AlignSelf<T>(this T v, Align align) where T : View => (T)(V(v) with { SelfAlign = align });

    public static T Background<T>(this T v, ColorValue color) where T : View => (T)(V(v) with { Background = color });
    public static T Border<T>(this T v, ColorValue color, float width = 1) where T : View =>
        (T)(V(v) with { BorderColor = color, BorderWidth = width });
    public static T Radius<T>(this T v, float radius) where T : View => (T)(V(v) with { CornerRadius = radius });

    public static T Foreground<T>(this T v, ColorValue color) where T : View => (T)(V(v) with { Foreground = color });
    public static T FontSize<T>(this T v, float size) where T : View => (T)(V(v) with { FontSize = size });
    public static T Bold<T>(this T v, bool bold = true) where T : View => (T)(V(v) with { Bold = bold });
    public static T Font<T>(this T v, TextStyle style) where T : View => (T)(V(v) with { TextStyle = style });
    /// <summary>Style de texte du thème par son nom, y compris ceux ajoutés par <c>WithTextStyle</c>.</summary>
    public static T Font<T>(this T v, string styleName) where T : View => (T)(V(v) with { TextStyleName = styleName });

    /// <summary>
    /// Habille cette vue et ses descendants avec une famille du catalogue, par son nom :
    /// <c>Button("OK").Theme("Sable")</c>. Clair ou sombre selon le mode en cours ; null = thème du parent.
    /// </summary>
    public static T Theme<T>(this T v, string? name) where T : View => (T)(V(v) with { ThemeName = name });
    /// <summary>Même chose avec une famille (sans passer par son nom).</summary>
    public static T Theme<T>(this T v, ThemeFamily family) where T : View => (T)(V(v) with { ThemeFamily = family });
    /// <summary>Un thème précis, tel quel (sa variante claire ou sombre ne change pas avec le mode).</summary>
    public static T Theme<T>(this T v, Theme theme) where T : View => (T)(V(v) with { Theme = theme });
    /// <summary>Force clair/sombre sur cette branche ; null = suit le parent (ou le système).</summary>
    public static T ColorScheme<T>(this T v, ColorScheme? scheme) where T : View => (T)(V(v) with { Scheme = scheme });

    public static T OnTap<T>(this T v, Action action) where T : View => (T)(V(v) with { TapAction = action });
    public static T OnTap<T>(this T v, Func<Task> action) where T : View => (T)(V(v) with { TapAction = Ui.Async(action) });

    /// <summary>Pose une vue par-dessus (badge, voile…), alignée dans les bornes de celle-ci.</summary>
    public static ZStackView Overlay(this View v, View overlay, Alignment? alignment = null) =>
        Ui.ZStack(v, overlay) with { Alignment = alignment ?? Primitives.Alignment.Center };

    // Spécifiques
    public static StackView Spacing(this StackView s, float spacing) => s with { Spacing = spacing };
    public static StackView Alignment(this StackView s, Align align) => s with { CrossAlign = align };
    public static ZStackView Alignment(this ZStackView z, Alignment alignment) => z with { Alignment = alignment };
    public static TextView TextAlignment(this TextView t, TextAlign align) => t with { TextAlign = align };
    public static ButtonView Style(this ButtonView b, ButtonStyle style) => b with { Style = style };
    public static ButtonView Style(this ButtonView b, ButtonVariant variant) => b with { Variant = variant };
    /// <summary>Style de bouton du thème par son nom, y compris ceux ajoutés par <c>WithButtonStyle</c>.</summary>
    public static ButtonView Style(this ButtonView b, string styleName) => b with { StyleName = styleName };

    // Apparence sur mesure d'un seul contrôle (pour tous : le Template du thème).
    public static ButtonView Template(this ButtonView b, Func<ButtonState, View> template) => b with { CustomTemplate = template };
    public static CheckboxView Template(this CheckboxView c, Func<CheckboxState, View> template) => c with { CustomTemplate = template };
    public static TextFieldView Template(this TextFieldView f, Func<TextFieldState, View> template) => f with { CustomTemplate = template };

    // Éléments de menu.
    /// <summary>Raccourci clavier, actif même menu fermé : <c>.Shortcut("Ctrl+N")</c> (Cmd+N sur macOS).</summary>
    public static MenuItemView Shortcut(this MenuItemView item, string gesture) => item with { Gesture = KeyGesture.Parse(gesture) };
    public static MenuItemView Shortcut(this MenuItemView item, KeyGesture gesture) => item with { Gesture = gesture };
    /// <summary>Élément grisé quand <paramref name="enabled"/> est faux : ni clic ni raccourci.</summary>
    public static MenuItemView Enabled(this MenuItemView item, bool enabled = true) => item with { IsEnabled = enabled };
    /// <summary>Coche devant le libellé (choix exclusifs : une coche sur l'élément choisi).</summary>
    public static MenuItemView Checked(this MenuItemView item, bool isChecked = true) => item with { IsChecked = isChecked };
}
