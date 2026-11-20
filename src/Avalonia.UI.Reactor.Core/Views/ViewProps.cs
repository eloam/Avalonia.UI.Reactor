namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Recopie des propriétés communes à toutes les vues (mise en page, style, thème, action).
/// Seul endroit à compléter quand <see cref="View"/> reçoit une propriété : un test vérifie qu'aucune n'est oubliée.
/// </summary>
internal static class ViewProps
{
    public static T CopyFrom<T>(this T target, View source) where T : View => (T)((View)target with
    {
        Key = source.Key,
        Padding = source.Padding,
        Width = source.Width,
        Height = source.Height,
        Grow = source.Grow,
        SelfAlign = source.SelfAlign,
        Background = source.Background,
        BorderColor = source.BorderColor,
        BorderWidth = source.BorderWidth,
        CornerRadius = source.CornerRadius,
        Foreground = source.Foreground,
        TextStyle = source.TextStyle,
        TextStyleName = source.TextStyleName,
        FontSize = source.FontSize,
        Bold = source.Bold,
        Theme = source.Theme,
        ThemeName = source.ThemeName,
        ThemeFamily = source.ThemeFamily,
        Scheme = source.Scheme,
        TapAction = source.TapAction,
    });

    /// <summary>Vue sans aucune propriété commune renseignée (aucun modificateur).</summary>
    public static bool HasNoModifiers(this View view) => view.CopyFrom(Blank) == view;

    static readonly BoxView Blank = new();
}
