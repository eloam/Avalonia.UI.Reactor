// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;

namespace MartletUI.Widgets;

/// <summary>
/// Provides the look (<see cref="ControlTheme"/>) of our controls: their structure only.
/// </summary>
/// <remarks>
/// There is no color and no dimension here: the adapter of each control sets them from the resolved theme of its branch,
/// state by state (rest, hover, pressed, focus). The Fluent theme of Avalonia only serves the infrastructure (the window,
/// scrolling inside a field).
/// </remarks>
internal static class MartletThemes
{
    // The names of the template parts that the Avalonia base classes look up (Button, TextBox), and of the frame.
    private const string ContentPresenterPart = "PART_ContentPresenter";
    private const string ScrollViewerPart = "PART_ScrollViewer";
    private const string TextPresenterPart = "PART_TextPresenter";
    private const string FramePart = "PART_Frame";

    /// <summary>
    /// Gets the look of a button: background, border and label on a single presenter.
    /// </summary>
    public static ControlTheme Button { get; } = new(typeof(MartletButton))
    {
        Setters =
        {
            new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Hand)),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<MartletButton>((_, scope) =>
                Named(scope, ContentPresenterPart, new ContentPresenter()
                    .Bound(ContentPresenter.BackgroundProperty, TemplatedControl.BackgroundProperty)
                    .Bound(ContentPresenter.BorderBrushProperty, TemplatedControl.BorderBrushProperty)
                    .Bound(ContentPresenter.BorderThicknessProperty, TemplatedControl.BorderThicknessProperty)
                    .Bound(ContentPresenter.CornerRadiusProperty, TemplatedControl.CornerRadiusProperty)
                    .Bound(ContentPresenter.PaddingProperty, TemplatedControl.PaddingProperty)
                    .Bound(ContentPresenter.ForegroundProperty, TemplatedControl.ForegroundProperty)
                    .Bound(ContentPresenter.ContentProperty, ContentControl.ContentProperty)
                    .Bound(ContentPresenter.ContentTemplateProperty, ContentControl.ContentTemplateProperty)
                    .Bound(ContentPresenter.HorizontalContentAlignmentProperty, ContentControl.HorizontalContentAlignmentProperty)
                    .Bound(ContentPresenter.VerticalContentAlignmentProperty, ContentControl.VerticalContentAlignmentProperty)))),
        },
    };

    /// <summary>
    /// Gets the look of a checkbox: the box drawn by the theme, then the label.
    /// </summary>
    public static ControlTheme CheckBox { get; } = new(typeof(MartletCheckBox))
    {
        Setters =
        {
            new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Hand)),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<MartletCheckBox>((_, scope) =>
                Named(scope, MartletCheckBox.LayoutPart, new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        Named(scope, MartletCheckBox.GlyphPart, new CheckGlyph { VerticalAlignment = VerticalAlignment.Center }),
                        Named(scope, ContentPresenterPart, new ContentPresenter { VerticalAlignment = VerticalAlignment.Center }
                            .Bound(ContentPresenter.ContentProperty, ContentControl.ContentProperty)
                            .Bound(ContentPresenter.ForegroundProperty, TemplatedControl.ForegroundProperty)),
                    },
                }))),
        },
    };

    /// <summary>
    /// Gets the look of a text field: the frame, then the text scrolling inside the padding, with the placeholder while
    /// the field is empty.
    /// </summary>
    /// <remarks>
    /// The structure follows the template of the <see cref="TextBox"/> of Avalonia 12 (Simple theme): a text presenter
    /// bound to the field and a scroll viewer without scroll bars, found by the base class through their names.
    /// </remarks>
    public static ControlTheme TextBox { get; } = new(typeof(MartletTextBox))
    {
        Setters =
        {
            new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Ibeam)),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<MartletTextBox>((_, scope) =>
                Named(scope, FramePart, new Border { Child = EditingArea(scope).Bound(Layoutable.MarginProperty, TemplatedControl.PaddingProperty) }
                    .Bound(Border.BackgroundProperty, TemplatedControl.BackgroundProperty)
                    .Bound(Border.BorderBrushProperty, TemplatedControl.BorderBrushProperty)
                    .Bound(Border.BorderThicknessProperty, TemplatedControl.BorderThicknessProperty)
                    .Bound(Border.CornerRadiusProperty, TemplatedControl.CornerRadiusProperty)))),
        },
    };

    /// <summary>
    /// Gets the look of a text field whose template draws its appearance: the view of the template, then the editing
    /// area, placed by <see cref="EditorLayout"/> on the <c>field.Editor</c> slot of the template.
    /// </summary>
    public static ControlTheme TemplatedTextBox { get; } = new(typeof(MartletTextBox))
    {
        Setters =
        {
            new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Ibeam)),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<MartletTextBox>((_, scope) =>
                new EditorLayout { Children = { Named(scope, MartletTextBox.CustomPart, new Decorator()), EditingArea(scope) } })),
        },
    };

    // The editing area of a field: the text (bound to the field) and the placeholder, scrolling without scroll bars.
    private static ScrollViewer EditingArea(INameScope scope) => Named(scope, ScrollViewerPart, new ScrollViewer
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
        VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        Content = new Panel
        {
            Children =
            {
                Named(scope, MartletTextBox.PlaceholderPart, new TextBlock { VerticalAlignment = VerticalAlignment.Center }
                    .Bound(TextBlock.TextProperty, Avalonia.Controls.TextBox.PlaceholderTextProperty)
                    .Bound(TextBlock.ForegroundProperty, Avalonia.Controls.TextBox.PlaceholderForegroundProperty)),
                Named(scope, TextPresenterPart, new TextPresenter { VerticalAlignment = VerticalAlignment.Center }
                    .Bound(TextPresenter.TextProperty, Avalonia.Controls.TextBox.TextProperty, BindingMode.TwoWay)
                    .Bound(TextPresenter.CaretIndexProperty, Avalonia.Controls.TextBox.CaretIndexProperty)
                    .Bound(TextPresenter.SelectionStartProperty, Avalonia.Controls.TextBox.SelectionStartProperty)
                    .Bound(TextPresenter.SelectionEndProperty, Avalonia.Controls.TextBox.SelectionEndProperty)
                    .Bound(TextPresenter.CaretBrushProperty, Avalonia.Controls.TextBox.CaretBrushProperty)
                    .Bound(TextPresenter.CaretBlinkIntervalProperty, Avalonia.Controls.TextBox.CaretBlinkIntervalProperty)
                    .Bound(TextPresenter.SelectionBrushProperty, Avalonia.Controls.TextBox.SelectionBrushProperty)
                    .Bound(TextPresenter.SelectionForegroundBrushProperty, Avalonia.Controls.TextBox.SelectionForegroundBrushProperty)
                    .Bound(TextPresenter.PasswordCharProperty, Avalonia.Controls.TextBox.PasswordCharProperty)
                    .Bound(TextPresenter.RevealPasswordProperty, Avalonia.Controls.TextBox.RevealPasswordProperty)
                    .Bound(TextPresenter.TextAlignmentProperty, Avalonia.Controls.TextBox.TextAlignmentProperty)
                    .Bound(TextPresenter.TextWrappingProperty, Avalonia.Controls.TextBox.TextWrappingProperty)
                    .Bound(TextPresenter.LineHeightProperty, Avalonia.Controls.TextBox.LineHeightProperty)
                    .Bound(TextPresenter.LetterSpacingProperty, Avalonia.Controls.TextBox.LetterSpacingProperty)),
            },
        },
    });

    // Binds a property of a template part to a property of the control that owns the template.
    private static T Bound<T>(this T part, AvaloniaProperty property, AvaloniaProperty source, BindingMode mode = BindingMode.OneWay)
        where T : Control
    {
        part.Bind(property, new TemplateBinding(source) { Mode = mode });
        return part;
    }

    // Registers a named template part, which the control finds by its name.
    private static T Named<T>(INameScope scope, string name, T part)
        where T : Control
    {
        part.Name = name;
        scope.Register(name, part);
        return part;
    }
}
