using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Avalonia.UI.Reactor.Widgets;

/// <summary>
/// Apparence (<see cref="ControlTheme"/>) de nos contrôles : uniquement leur structure. Aucune couleur ni dimension ici :
/// l'adaptateur de chaque contrôle les pose à partir du thème résolu de sa branche, état par état (repos, survol, appui,
/// focus), comme le faisait le dessin de l'ancien moteur. Le thème Fluent d'Avalonia ne sert plus qu'à l'infrastructure
/// (fenêtre, défilement dans un champ).
/// </summary>
internal static class ReactorThemes
{
    /// <summary>Le fond, la bordure et le libellé d'un bouton, sur un seul présentateur.</summary>
    public static ControlTheme Button { get; } = new(typeof(ReactorButton))
    {
        Setters =
        {
            new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Hand)),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ReactorButton>((_, scope) =>
                Named(scope, "PART_ContentPresenter", new ContentPresenter()
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

    /// <summary>La case dessinée par le thème, puis le libellé.</summary>
    public static ControlTheme CheckBox { get; } = new(typeof(ReactorCheckBox))
    {
        Setters =
        {
            new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Hand)),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ReactorCheckBox>((_, scope) =>
                Named(scope, "PART_Layout", new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        Named(scope, "PART_Glyph", new CheckGlyph { VerticalAlignment = VerticalAlignment.Center }),
                        Named(scope, "PART_ContentPresenter", new ContentPresenter { VerticalAlignment = VerticalAlignment.Center }
                            .Bound(ContentPresenter.ContentProperty, ContentControl.ContentProperty)
                            .Bound(ContentPresenter.ForegroundProperty, TemplatedControl.ForegroundProperty)),
                    },
                }))),
        },
    };

    /// <summary>
    /// Cadre du champ, puis le texte défilant dans la marge intérieure, avec le texte indicatif quand il est vide.
    /// Structure reprise du modèle de <see cref="TextBox"/> d'Avalonia 12 (thème Simple) : un
    /// <c>PART_TextPresenter</c> lié au champ et un <c>PART_ScrollViewer</c> sans barres.
    /// </summary>
    public static ControlTheme TextBox { get; } = new(typeof(ReactorTextBox))
    {
        Setters =
        {
            new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Ibeam)),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ReactorTextBox>((_, scope) =>
                Named(scope, "PART_Frame", new Border
                {
                    Child = Named(scope, "PART_ScrollViewer", new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                        Content = new Panel
                        {
                            Children =
                            {
                                Named(scope, "PART_Placeholder", new TextBlock { VerticalAlignment = VerticalAlignment.Center }
                                    .Bound(TextBlock.TextProperty, Avalonia.Controls.TextBox.PlaceholderTextProperty)
                                    .Bound(TextBlock.ForegroundProperty, Avalonia.Controls.TextBox.PlaceholderForegroundProperty)),
                                Named(scope, "PART_TextPresenter", new TextPresenter { VerticalAlignment = VerticalAlignment.Center }
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
                    }.Bound(Layoutable.MarginProperty, TemplatedControl.PaddingProperty)),
                }
                .Bound(Border.BackgroundProperty, TemplatedControl.BackgroundProperty)
                .Bound(Border.BorderBrushProperty, TemplatedControl.BorderBrushProperty)
                .Bound(Border.BorderThicknessProperty, TemplatedControl.BorderThicknessProperty)
                .Bound(Border.CornerRadiusProperty, TemplatedControl.CornerRadiusProperty)))),
        },
    };

    /// <summary>Lie une propriété d'une partie du modèle à celle du contrôle qui porte le modèle.</summary>
    static T Bound<T>(this T part, AvaloniaProperty property, AvaloniaProperty source, BindingMode mode = BindingMode.OneWay)
        where T : Control
    {
        part.Bind(property, new TemplateBinding(source) { Mode = mode });
        return part;
    }

    /// <summary>Partie nommée du modèle, que le contrôle retrouve par son nom.</summary>
    static T Named<T>(INameScope scope, string name, T part) where T : Control
    {
        part.Name = name;
        scope.Register(name, part);
        return part;
    }
}
