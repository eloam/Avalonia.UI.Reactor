// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents a checkbox dressed by <see cref="ReactorThemes.CheckBox"/>: the box is drawn by <see cref="CheckGlyph"/>
/// with the theme of the branch, followed by the label.
/// </summary>
/// <remarks>
/// The size of the box and the space before the label also come from the theme.
/// </remarks>
[TemplatePart(LayoutPart, typeof(StackPanel))]
[TemplatePart(GlyphPart, typeof(CheckGlyph))]
internal sealed class ReactorCheckBox : CheckBox
{
    /// <summary>
    /// The name of the template part that lays out the box and the label.
    /// </summary>
    public const string LayoutPart = "PART_Layout";

    /// <summary>
    /// The name of the template part that draws the box.
    /// </summary>
    public const string GlyphPart = "PART_Glyph";

    private StackPanel? _layout;
    private CheckGlyph? _glyph;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorCheckBox"/> class.
    /// </summary>
    public ReactorCheckBox() => Theme = ReactorThemes.CheckBox;

    /// <summary>
    /// Gets or sets the theme of the branch, set by the adapter: colors, size of the box, space before the label.
    /// </summary>
    public Theme? Look
    {
        get;
        set
        {
            field = value;
            Refresh();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the box drawn by the theme is shown; <see langword="false"/> when a template
    /// provides the whole look (box and label) in the content.
    /// </summary>
    public bool ShowsGlyph
    {
        get;
        set
        {
            field = value;
            Refresh();
        }
    } = true;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<StackPanel>(LayoutPart);
        _glyph = e.NameScope.Find<CheckGlyph>(GlyphPart);
        Refresh();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty || change.Property == IsPointerOverProperty)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (_glyph is not null)
        {
            _glyph.IsVisible = ShowsGlyph;
        }

        if (Look is not { } theme)
        {
            return;
        }

        if (_layout is not null)
        {
            _layout.Spacing = Content is null || !ShowsGlyph ? 0 : theme.Checkbox.LabelSpacing;
        }

        if (ShowsGlyph)
        {
            _glyph?.Show(theme, IsChecked == true, IsPointerOver);
        }
    }
}
