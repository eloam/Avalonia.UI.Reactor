// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Copies the properties shared by all views: layout, style, theme and action.
/// </summary>
/// <remarks>
/// This is the only place to update when <see cref="View"/> gets a new property; a test checks that none is forgotten.
/// </remarks>
internal static class ViewProps
{
    private static readonly BoxView s_blank = new();

    /// <summary>
    /// Returns a copy of a view with the shared properties of another view.
    /// </summary>
    /// <typeparam name="T">The type of the target view.</typeparam>
    /// <param name="target">The view that receives the properties.</param>
    /// <param name="source">The view whose properties are copied.</param>
    /// <returns>A copy of <paramref name="target"/> with the shared properties of <paramref name="source"/>.</returns>
    public static T CopyFrom<T>(this T target, View source)
        where T : View => (T)((View)target with
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
            AppearAction = source.AppearAction,
            DisappearAction = source.DisappearAction,
        });

    /// <summary>
    /// Determines whether a view has none of the shared properties set, that is, no modifier.
    /// </summary>
    /// <param name="view">The view to check.</param>
    /// <returns><see langword="true"/> if no shared property is set; otherwise, <see langword="false"/>.</returns>
    public static bool HasNoModifiers(this View view) => view.CopyFrom(s_blank) == view;
}
