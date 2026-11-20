using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Champ de saisie habillé par <see cref="ReactorThemes.TextBox"/>. Sa largeur naturelle ne dépend pas du texte saisi :
/// elle vient du thème (<c>MinWidth</c>) ; un long texte défile dans le champ au lieu de l'élargir et de décaler toute la
/// ligne, comme dans l'ancien moteur. Le champ s'élargit seulement par la mise en page (<c>.Grow()</c>, <c>.FillWidth()</c>).
/// Le texte indicatif n'est affiché que si le champ est vide. Un clic droit ouvre le menu couper / copier / coller.
/// </summary>
internal sealed class ReactorTextBox : TextBox
{
    readonly MenuItem _cut = new(), _copy = new(), _paste = new();
    TextBlock? _placeholder;

    public ReactorTextBox()
    {
        Theme = ReactorThemes.TextBox;
        _cut.Click += (_, _) => Cut();
        _copy.Click += (_, _) => Copy();
        _paste.Click += (_, _) => Paste();
        var menu = new MenuFlyout { Items = { _cut, _copy, _paste } };
        menu.Opening += (_, _) =>
        {
            _cut.IsEnabled = CanCut;
            _copy.IsEnabled = CanCopy;
            _paste.IsEnabled = CanPaste;
        };
        ContextFlyout = menu;
    }

    /// <summary>Libellés du menu contextuel, pris dans le thème de la branche.</summary>
    public void SetEditMenu(EditMenuLabels labels)
    {
        _cut.Header = labels.Cut;
        _copy.Header = labels.Copy;
        _paste.Header = labels.Paste;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _placeholder = e.NameScope.Find<TextBlock>("PART_Placeholder");
        ShowPlaceholder();
    }

    /// <summary>Raccourcis affichés dans le menu : ceux de la plateforme (Cmd sur macOS, Ctrl ailleurs).</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (this.GetPlatformSettings()?.HotkeyConfiguration is not { } keys) return;
        _cut.InputGesture = keys.Cut.FirstOrDefault();
        _copy.InputGesture = keys.Copy.FirstOrDefault();
        _paste.InputGesture = keys.Paste.FirstOrDefault();
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
