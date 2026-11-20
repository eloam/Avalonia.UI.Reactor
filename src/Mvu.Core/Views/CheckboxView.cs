namespace Mvu.Views;

public sealed record CheckboxView(bool IsChecked, Action<bool>? OnChange, string? Label) : View
{
    /// <summary>Apparence sur mesure pour cette case, prioritaire sur celle du thème.</summary>
    public Func<CheckboxState, View>? CustomTemplate { get; init; }
}
