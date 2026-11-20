using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary>
/// <c>TextField</c> : un <see cref="TextBox"/> d'Avalonia (sélection, copier-coller, annuler, IME), aux couleurs du thème.
/// Le champ est contrôlé : chaque saisie part vers l'état (<c>OnChange</c>), puis le champ affiche la valeur de la vue
/// — un état qui refuse ou transforme la saisie l'emporte donc sur ce qui a été tapé. Entrée exécute <c>OnSubmit</c>.
/// </summary>
internal sealed class TextFieldAdapter : ViewAdapter<TextFieldView, FieldBox>
{
    public static readonly TextFieldAdapter Instance = new();

    public override bool DrawsDecoration => true;

    protected override void Attach(Reconciler reconciler, Node node, FieldBox field)
    {
        // La propriété Text, pas l'événement TextChanged : Avalonia publie celui-ci plus tard sur le répartiteur, et une
        // action qui suit aussitôt la saisie (Entrée, clic, raccourci) lirait encore l'état d'avant.
        field.PropertyChanged += (_, e) =>
        {
            if (e.Property != TextBox.TextProperty || node.Applying) return;
            string text = field.Text ?? "";
            reconciler.Run(() => ((TextFieldView)node.View).OnChange?.Invoke(text));
            Reconciler.Apply(node, () =>
            {
                string shown = ((TextFieldView)node.View).Value;
                if (field.Text != shown) field.Text = shown;
            });
        };
        // Avant le champ (tunnel) : un champ d'une ligne n'a rien à faire d'Entrée, la vue si.
        field.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.Enter || ((TextFieldView)node.View).OnSubmit is not { } submit) return;
            e.Handled = true;
            reconciler.Run(submit);
        }, RoutingStrategies.Tunnel);
    }

    protected override void Update(Reconciler reconciler, Node node, FieldBox field, TextFieldView view, ViewEnvironment env)
    {
        var theme = env.Theme;
        var style = theme.TextField;
        var padding = view.Padding ?? style.Padding;

        if (field.Text != view.Value) field.Text = view.Value;
        field.PlaceholderText = view.Placeholder;
        field.SetFont(env.FontFor(view));
        field.SetBrush(TemplatedControl.BackgroundProperty, theme[view.Background ?? style.Background]);
        field.SetBrush(TemplatedControl.ForegroundProperty, theme[view.Foreground ?? style.Foreground]);
        field.SetBrush(TextBox.PlaceholderForegroundProperty, theme[style.Placeholder]);
        field.SetBrush(TextBox.CaretBrushProperty, theme[style.Caret]);
        field.SetBrush(TemplatedControl.BorderBrushProperty, theme[view.BorderColor ?? style.BorderColor]);
        field.BorderThickness = new Thickness(view.BorderWidth ?? style.BorderWidth);
        field.CornerRadius = new CornerRadius(view.CornerRadius ?? style.Radius);
        field.Padding = padding.ToAvalonia();
        // Largeur naturelle du thème (le texte saisi ne la change pas) ; hauteur : la ligne de texte et la marge.
        field.MinWidth = style.MinWidth + padding.Horizontal;
        field.MinHeight = 0;
        field.VerticalContentAlignment = VerticalAlignment.Center;
    }
}
