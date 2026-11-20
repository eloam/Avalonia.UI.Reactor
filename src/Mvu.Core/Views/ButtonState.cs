using Mvu.Theming;

namespace Mvu.Views;

/// <summary>
/// Ce que reçoit un modèle de bouton : son libellé, le style choisi (couleurs du thème)
/// et son état d'interaction.
/// <code>
/// Template = b => HStack(Text("★"), Text(b.Label))
///     .Padding(12, 6)
///     .Background(b.Pressed ? ColorRole.Accent : b.Variant.Background)
///     .Radius(999)
/// </code>
/// </summary>
public sealed record ButtonState(string Label, ButtonVariant Variant, bool Hovered, bool Pressed);
