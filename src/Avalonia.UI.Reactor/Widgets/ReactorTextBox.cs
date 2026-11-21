// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents a text field dressed by <see cref="ReactorThemes.TextBox"/>.
/// </summary>
/// <remarks>
/// The natural width of the field does not depend on the typed text: it comes from the theme (<c>MinWidth</c>), and a
/// long text scrolls inside the field instead of widening it and shifting the whole row. The field only widens through
/// layout (<c>.Grow()</c>, <c>.FillWidth()</c>). The placeholder is shown only while the field is empty. A right click
/// opens the cut / copy / paste menu.
/// </remarks>
[TemplatePart(PlaceholderPart, typeof(TextBlock))]
[TemplatePart(CustomPart, typeof(Decorator))]
internal sealed class ReactorTextBox : TextBox
{
    /// <summary>
    /// The name of the template part that shows the placeholder.
    /// </summary>
    public const string PlaceholderPart = "PART_Placeholder";

    /// <summary>
    /// The name of the template part that holds the view of a template.
    /// </summary>
    public const string CustomPart = "PART_Custom";

    private readonly MenuItem _cut = new();
    private readonly MenuItem _copy = new();
    private readonly MenuItem _paste = new();
    private TextBlock? _placeholder;
    private Decorator? _custom;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorTextBox"/> class, with its context menu.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the view of the template of the field (with <see cref="ReactorThemes.TemplatedTextBox"/>): it draws
    /// the frame, and the editing area is placed on its <see cref="EditorSlot"/>; <see langword="null"/> without a
    /// template.
    /// </summary>
    public Control? CustomContent
    {
        get;
        set
        {
            if (ReferenceEquals(value, field))
            {
                return;
            }

            field = value;
            ShowCustomContent();
        }
    }

    /// <summary>
    /// Sets the labels of the context menu, taken from the theme of the branch.
    /// </summary>
    /// <param name="labels">The labels of the cut, copy and paste commands.</param>
    public void SetEditMenu(EditMenuLabels labels)
    {
        _cut.Header = labels.Cut;
        _copy.Header = labels.Copy;
        _paste.Header = labels.Paste;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _placeholder = e.NameScope.Find<TextBlock>(PlaceholderPart);
        if (_custom is not null)
        {
            _custom.Child = null;
        }

        _custom = e.NameScope.Find<Decorator>(CustomPart);
        ShowCustomContent();
        ShowPlaceholder();
    }

    /// <summary>
    /// Shows the shortcuts of the platform in the context menu (Cmd on macOS, Ctrl elsewhere).
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (this.GetPlatformSettings()?.HotkeyConfiguration is not { } keys)
        {
            return;
        }

        _cut.InputGesture = keys.Cut.FirstOrDefault();
        _copy.InputGesture = keys.Copy.FirstOrDefault();
        _paste.InputGesture = keys.Paste.FirstOrDefault();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            ShowPlaceholder();
        }
    }

    /// <summary>
    /// Measures the field: without a template, the natural width is that of the theme (<c>MinWidth</c>); with one, that of
    /// the view of the template.
    /// </summary>
    /// <param name="availableSize">The space available to the field.</param>
    /// <returns>The natural size of the field.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size size = base.MeasureOverride(availableSize);
        return CustomContent is null ? size.WithWidth(0) : size;
    }

    private void ShowCustomContent()
    {
        if (_custom is null || ReferenceEquals(_custom.Child, CustomContent))
        {
            return;
        }

        if (CustomContent?.Parent is Decorator previous)
        {
            previous.Child = null;
        }

        _custom.Child = CustomContent;
    }

    private void ShowPlaceholder()
    {
        if (_placeholder is not null)
        {
            _placeholder.IsVisible = string.IsNullOrEmpty(Text);
        }
    }
}
