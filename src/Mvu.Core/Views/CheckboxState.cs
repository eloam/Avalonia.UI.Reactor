namespace Mvu.Views;

/// <summary>Ce que reçoit un modèle de case à cocher : coché ou non, son libellé et son état d'interaction.</summary>
public sealed record CheckboxState(bool IsChecked, string? Label, bool Hovered, bool Pressed);
