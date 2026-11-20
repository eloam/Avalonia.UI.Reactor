namespace Mvu.Views;

public sealed record TextFieldView(string Value, Action<string>? OnChange) : View
{
    public string Placeholder { get; init; } = "";
    public Action? OnSubmit { get; init; }
    /// <summary>Apparence sur mesure pour ce champ, prioritaire sur celle du thème.</summary>
    public Func<TextFieldState, View>? CustomTemplate { get; init; }
}
