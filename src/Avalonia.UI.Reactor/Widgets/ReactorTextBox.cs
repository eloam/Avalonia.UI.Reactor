using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Champ de saisie habillé par <see cref="ReactorThemes.TextBox"/>. Sa largeur naturelle ne dépend pas du texte saisi :
/// elle vient du thème (<c>MinWidth</c>) ; un long texte défile dans le champ au lieu de l'élargir et de décaler toute la
/// ligne, comme dans l'ancien moteur. Le champ s'élargit seulement par la mise en page (<c>.Grow()</c>, <c>.FillWidth()</c>).
/// Le texte indicatif n'est affiché que si le champ est vide.
/// </summary>
internal sealed class ReactorTextBox : TextBox
{
    TextBlock? _placeholder;

    public ReactorTextBox() => Theme = ReactorThemes.TextBox;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _placeholder = e.NameScope.Find<TextBlock>("PART_Placeholder");
        ShowPlaceholder();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty) ShowPlaceholder();
    }

    protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize).WithWidth(0);

    void ShowPlaceholder()
    {
        if (_placeholder is not null) _placeholder.IsVisible = string.IsNullOrEmpty(Text);
    }
}
