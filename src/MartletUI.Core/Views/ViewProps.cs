// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Globalization;

namespace MartletUI.Views;

/// <summary>
/// Copies the properties shared by all views: layout, style, theme and action.
/// </summary>
/// <remarks>
/// These are the only places to update when <see cref="View"/> gets a new property (<see cref="CopyFrom"/> and
/// <see cref="Inherit"/>); a test checks that none is forgotten.
/// </remarks>
internal static class ViewProps
{
    // Separates the key of a group from the key, or the position, of one of its children.
    private const char GroupKeySeparator = '/';

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
            TaskAction = source.TaskAction,
            TaskKey = source.TaskKey,
        });

    /// <summary>
    /// Determines whether a view has none of the shared properties set, that is, no modifier.
    /// </summary>
    /// <param name="view">The view to check.</param>
    /// <returns><see langword="true"/> if no shared property is set; otherwise, <see langword="false"/>.</returns>
    public static bool HasNoModifiers(this View view) => view.CopyFrom(s_blank) == view;

    /// <summary>
    /// Returns a copy of a child of a group with the modifiers of the group, as SwiftUI applies the modifiers of a
    /// <c>Group</c> to each of its children.
    /// </summary>
    /// <remarks>
    /// The child keeps the values it sets itself; the group fills the others. The key of the group identifies the group:
    /// the key of the child is placed below it, or its position in the group when it has none.
    /// </remarks>
    /// <param name="child">A child of the group.</param>
    /// <param name="group">The group.</param>
    /// <param name="position">The position of the child in the group.</param>
    /// <returns>A copy of <paramref name="child"/> with the modifiers of <paramref name="group"/>.</returns>
    public static View Inherit(this View child, View group, int position) => child with
    {
        Key = group.Key is null
            ? child.Key
            : $"{group.Key}{GroupKeySeparator}{child.Key ?? position.ToString(CultureInfo.InvariantCulture)}",
        Padding = child.Padding ?? group.Padding,
        Width = child.Width ?? group.Width,
        Height = child.Height ?? group.Height,
        Grow = child.Grow > 0 ? child.Grow : group.Grow,
        SelfAlign = child.SelfAlign ?? group.SelfAlign,
        Background = child.Background ?? group.Background,
        BorderColor = child.BorderColor ?? group.BorderColor,
        BorderWidth = child.BorderWidth ?? group.BorderWidth,
        CornerRadius = child.CornerRadius ?? group.CornerRadius,
        Foreground = child.Foreground ?? group.Foreground,
        TextStyle = child.TextStyle ?? group.TextStyle,
        TextStyleName = child.TextStyleName ?? group.TextStyleName,
        FontSize = child.FontSize ?? group.FontSize,
        Bold = child.Bold ?? group.Bold,
        Theme = child.Theme ?? group.Theme,
        ThemeName = child.ThemeName ?? group.ThemeName,
        ThemeFamily = child.ThemeFamily ?? group.ThemeFamily,
        Scheme = child.Scheme ?? group.Scheme,
        TapAction = child.TapAction ?? group.TapAction,
        AppearAction = child.AppearAction ?? group.AppearAction,
        DisappearAction = child.DisappearAction ?? group.DisappearAction,
        TaskAction = child.TaskAction ?? group.TaskAction,
        TaskKey = child.TaskAction is null ? group.TaskKey : child.TaskKey,
    };
}
