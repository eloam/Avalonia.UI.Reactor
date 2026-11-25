// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Theming;

/// <summary>
/// Specifies a semantic color: views refer to roles, and the current theme gives them a value.
/// </summary>
public enum ColorRole
{
    /// <summary>
    /// The background of the window.
    /// </summary>
    Page,

    /// <summary>
    /// The background of cards, fields and menus, above the page.
    /// </summary>
    Surface,

    /// <summary>
    /// The color of borders and separators.
    /// </summary>
    Border,

    /// <summary>
    /// The border of text fields and checkboxes.
    /// </summary>
    FieldBorder,

    /// <summary>
    /// The main text color.
    /// </summary>
    Text,

    /// <summary>
    /// The color of secondary text: captions, placeholders, shortcuts.
    /// </summary>
    TextMuted,

    /// <summary>
    /// The accent color: primary buttons, focus, selection.
    /// </summary>
    Accent,

    /// <summary>
    /// The color of text and icons on the accent color.
    /// </summary>
    OnAccent,

    /// <summary>
    /// The background of secondary buttons.
    /// </summary>
    Secondary,

    /// <summary>
    /// The color of text on the secondary color.
    /// </summary>
    OnSecondary,

    /// <summary>
    /// The color of destructive actions and errors.
    /// </summary>
    Danger,

    /// <summary>
    /// The color of text on the danger color.
    /// </summary>
    OnDanger,
}
