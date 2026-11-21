// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents the colors of a button style.
/// </summary>
/// <remarks>
/// Hover and pressed states are drawn with an overlay of the theme text color: it darkens a light theme and lightens a
/// dark one.
/// </remarks>
/// <param name="Background">The background color.</param>
/// <param name="Foreground">The text color.</param>
/// <param name="BorderColor">The border color, or <see langword="null"/> for no border.</param>
/// <param name="HoverOverlay">The opacity of the overlay when the pointer is over the button.</param>
/// <param name="PressedOverlay">The opacity of the overlay when the button is pressed.</param>
public sealed record ButtonVariant(
    ColorValue Background,
    ColorValue Foreground,
    ColorValue? BorderColor = null,
    float HoverOverlay = 0.08f,
    float PressedOverlay = 0.16f);
