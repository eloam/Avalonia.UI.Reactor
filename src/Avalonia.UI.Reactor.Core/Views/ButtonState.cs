// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Theming;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Represents what a button template receives: the label, the colors of the chosen style and the interaction state.
/// </summary>
/// <example>
/// <code>
/// Template = b =&gt; HStack(Text("★"), Text(b.Label))
///     .Padding(12, 6)
///     .Background(b.Pressed ? ColorRole.Accent : b.Variant.Background)
///     .Radius(999)
/// </code>
/// </example>
/// <param name="Label">The text of the button.</param>
/// <param name="Variant">The colors of the style of the button.</param>
/// <param name="Hovered">A value indicating whether the pointer is over the button.</param>
/// <param name="Pressed">A value indicating whether the button is pressed.</param>
public sealed record ButtonState(string Label, ButtonVariant Variant, bool Hovered, bool Pressed);
