using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Case à cocher habillée par <see cref="ReactorThemes.CheckBox"/> : la case est dessinée par <see cref="CheckGlyph"/>
/// selon le thème de la branche, puis vient le libellé. Taille et espacement viennent aussi du thème.
/// </summary>
internal sealed class ReactorCheckBox : CheckBox
{
    StackPanel? _layout;
    CheckGlyph? _glyph;
    Theme? _look;
    bool _showsGlyph = true;

    public ReactorCheckBox() => Theme = ReactorThemes.CheckBox;

    /// <summary>Thème de la branche (posé par l'adaptateur) : couleurs, taille de la case, espacement du libellé.</summary>
    public Theme? Look
    {
        get => _look;
        set
        {
            _look = value;
            Refresh();
        }
    }

    /// <summary>Case dessinée par le thème ; faux quand un modèle fournit toute l'apparence (case et libellé) dans le contenu.</summary>
    public bool ShowsGlyph
    {
        get => _showsGlyph;
        set
        {
            _showsGlyph = value;
            Refresh();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<StackPanel>("PART_Layout");
        _glyph = e.NameScope.Find<CheckGlyph>("PART_Glyph");
        Refresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty || change.Property == IsPointerOverProperty) Refresh();
    }

    void Refresh()
    {
        if (_glyph is not null) _glyph.IsVisible = _showsGlyph;
        if (_look is not { } theme) return;
        if (_layout is not null) _layout.Spacing = Content is null || !_showsGlyph ? 0 : theme.Checkbox.LabelSpacing;
        if (_showsGlyph) _glyph?.Show(theme, IsChecked == true, IsPointerOver);
    }
}
