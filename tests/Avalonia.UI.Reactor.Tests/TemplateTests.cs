using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>Modèles de contrôles : l'apparence devient une vue, fonction de l'état du contrôle.</summary>
public class TemplateTests
{
    static readonly Color Red = Color.Hex("#FF0000"), Green = Color.Hex("#00FF00"), Blue = Color.Hex("#0000FF");

    /// <summary>Bouton dont le fond dépend de l'état : rouge au repos, vert au survol, bleu à l'appui.</summary>
    static readonly Theme Templated = Theme.Light.WithButton(b => b with
    {
        Template = s => Text(s.Label)
            .Padding(10)
            .Background(s.Pressed ? Blue : s.Hovered ? Green : Red),
    });

    [Fact]
    public void Theme_template_draws_buttons_and_follows_their_state()
    {
        var clicks = 0;
        using var t = new TestHost(new ViewProbe(() => VStack(Button("OK", () => clicks++))), themes: Templated);
        var b = t.BoundsOf(v => v is ButtonView);
        var inside = new Point(b.X + 3, b.Y + 3);

        Assert.Equal(Red, t.PixelAt(inside.X, inside.Y));

        t.Ui.PointerMove(inside);
        Assert.Equal(Green, t.PixelAt(inside.X, inside.Y));

        t.Ui.PointerDown(inside);
        Assert.Equal(Blue, t.PixelAt(inside.X, inside.Y));

        t.Ui.PointerUp(inside);
        Assert.Equal(1, clicks); // le comportement du bouton est intact
    }

    [Fact]
    public void Template_defines_the_size_of_the_control()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            Button("OK").Template(_ => Box().Size(120, 44)))));

        var b = t.BoundsOf(v => v is ButtonView);
        Assert.Equal(120, b.Width);
        Assert.Equal(44, b.Height);
    }

    [Fact]
    public void Template_on_a_view_wins_over_the_theme()
    {
        using var t = new TestHost(new ViewProbe(() => VStack(
            Button("OK").Template(s => Text(s.Label).Padding(10).Background(Blue)))), themes: Templated);
        var b = t.BoundsOf(v => v is ButtonView);
        Assert.Equal(Blue, t.PixelAt(b.X + 3, b.Y + 3));
    }

    [Fact]
    public void Checkbox_template_receives_the_checked_state()
    {
        var probe = new CheckProbe();
        using var t = new TestHost(probe);
        var box = t.BoundsOf(v => v is CheckboxView);

        Assert.Equal(Red, t.PixelAt(box.X + 5, box.Y + 5));
        t.Click(v => v is CheckboxView);
        Assert.True(probe.Checked);
        Assert.Equal(Green, t.PixelAt(box.X + 5, box.Y + 5));
    }

    [Fact]
    public void TextField_template_places_the_editor_and_editing_still_works()
    {
        var probe = new FieldProbe();
        using var t = new TestHost(probe);

        t.Click(v => v is TextFieldView);
        t.Type("bonjour");
        Assert.Equal("bonjour", probe.Value);

        // Le cadre du modèle est dessiné (fond bleu au focus) autour de la zone d'édition.
        var field = t.BoundsOf(v => v is TextFieldView);
        Assert.Equal(Blue, t.PixelAt(field.X + 1, field.Y + 1));

        // Un clic tout à gauche de la zone d'édition place le curseur au début.
        t.Ui.PointerDown(new Point(field.X + 13, field.Y + field.Height / 2));
        t.Ui.PointerUp(new Point(field.X + 13, field.Y + field.Height / 2));
        t.Type("«");
        Assert.Equal("«bonjour", probe.Value);
    }
}
