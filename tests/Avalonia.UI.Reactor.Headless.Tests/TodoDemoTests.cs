using Avalonia.Headless.XUnit;
using Avalonia.Input;
using TodoDemo;

namespace Avalonia.UI.Reactor.Headless.Tests;

using Avalonia.UI.Reactor.Headless.Tests.Probes;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Widgets;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>Scénarios utilisateur sur l'application de démonstration, rendue par le moteur Avalonia.</summary>
public class TodoDemoTests
{
    static ReactorTestHost Demo() => new(new TodoApp(), 560, 720, DemoThemes.Create());

    [AvaloniaFact]
    public void Counter_increments_decrements_and_resets()
    {
        using var t = Demo();

        t.ClickButton("+");
        t.ClickButton("+");
        t.ClickButton("−");
        Assert.True(t.HasText("1"));
        Assert.True(t.HasButton("Remettre à zéro"));

        t.ClickButton("Remettre à zéro");
        Assert.True(t.HasText("0"));
        Assert.False(t.HasButton("Remettre à zéro"));
    }

    [AvaloniaFact]
    public void Typing_then_Enter_adds_a_task_and_clears_the_field()
    {
        using var t = Demo();
        t.Click(t.Field);

        t.Type("Publier le POC");
        t.Press(Key.Enter, PhysicalKey.Enter);

        Assert.True(t.HasText("Publier le POC"));
        Assert.Equal("", t.Field.Text);
        Assert.True(t.HasText("2 restante(s)"));
    }

    [AvaloniaFact]
    public void Backspace_and_arrows_edit_at_the_caret()
    {
        using var t = Demo();
        t.Click(t.Field);

        t.Type("abd");
        t.Press(Key.Left, PhysicalKey.ArrowLeft);
        t.Type("c");
        t.Press(Key.End, PhysicalKey.End);
        t.Press(Key.Back, PhysicalKey.Backspace);

        Assert.Equal("abc", t.Field.Text);
    }

    [AvaloniaFact]
    public void Tapping_a_row_toggles_the_task()
    {
        using var t = Demo();

        t.Click(t.TextShowing("Ajouter un backend mobile"));
        Assert.True(t.HasText("0 restante(s)"));
        Assert.All(t.Controls.OfType<ReactorCheckBox>(), box => Assert.True(box.IsChecked));

        t.Click(t.TextShowing("Ajouter un backend mobile"));
        Assert.True(t.HasText("1 restante(s)"));
    }

    [AvaloniaFact]
    public void Async_action_shows_progress_then_result()
    {
        using var t = Demo();

        t.ClickButton("Idée");
        Assert.True(t.HasButton("…"));

        t.WaitUntil(() => t.HasButton("Idée"));
        Assert.True(t.HasText("2 restante(s)"));
    }

    [AvaloniaFact]
    public void Filters_and_purge()
    {
        using var t = Demo();

        t.ClickButton("Faites");
        Assert.True(t.HasText("Écrire le moteur de layout"));
        Assert.False(t.HasText("Ajouter un backend mobile"));

        t.ClickButton("Actives");
        Assert.False(t.HasText("Écrire le moteur de layout"));
        Assert.True(t.HasText("Ajouter un backend mobile"));

        t.ClickButton("Toutes");
        t.ClickButton("Purger");
        Assert.False(t.HasText("Écrire le moteur de layout"));
        Assert.True(t.HasText("Ajouter un backend mobile"));
        Assert.False(t.HasButton("Purger"));
    }

    [AvaloniaFact]
    public void Changing_theme_keeps_child_component_state()
    {
        using var t = Demo();
        t.ClickButton("+");

        t.ClickButton("Sombre");
        Assert.Equal(Theme.Dark.Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());
        Assert.True(t.HasText("1"));

        t.ClickButton("+");
        Assert.True(t.HasText("2"));
    }

    [AvaloniaFact]
    public void The_theme_choice_is_plain_state()
    {
        var catalog = DemoThemes.Create();
        using var t = new ReactorTestHost(new TodoApp(), 560, 720, catalog);
        foreach (var label in new[] { "Standard", "Sable", "Système", "Clair", "Sombre" }) Assert.True(t.HasButton(label), label);
        var sable = catalog.FamilyNamed("Sable")!;

        t.ClickButton("Sable");
        Assert.Equal(sable.For(ColorScheme.Light).Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());

        t.ClickButton("Sombre");
        Assert.Equal(sable.For(ColorScheme.Dark).Palette.Page.ToAvaloniaColor(), t.Host.Background.ColorOf());
    }

    [AvaloniaFact]
    public void Only_what_depends_on_the_changed_state_is_recomputed()
    {
        using var t = Demo();

        t.ClickButton("+");
        Assert.Equal(1, t.Stats.Evaluated); // la carte compteur seule
        Assert.True(t.Stats.Reused >= 1);   // la liste de tâches est reprise telle quelle

        t.Click(t.Field);
        t.Type("x");
        Assert.Equal(1, t.Stats.Evaluated); // la ligne de saisie seule

        t.Click(t.TextShowing("Ajouter un backend mobile"));
        Assert.Equal(3, t.Stats.Evaluated); // la ligne cochée, le pied « N restante(s) » et l'anneau de progression
    }

    [AvaloniaFact]
    public void The_progress_ring_shows_its_percentage()
    {
        using var t = new ReactorTestHost(new ViewProbe(() => VStack(TodoDemo.Widgets.ProgressRing(1, 4))));

        Assert.True(t.HasText("25%"));
    }
}
