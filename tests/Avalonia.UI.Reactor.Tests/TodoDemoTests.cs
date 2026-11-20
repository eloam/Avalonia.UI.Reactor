using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>Scénarios utilisateur sur l'application de démonstration.</summary>
public class TodoDemoTests
{
    [Fact]
    public void Counter_increments_decrements_and_resets()
    {
        using var t = new TestHost(new TodoApp(), themes: DemoThemes.Create());

        t.ClickButton("+");
        t.ClickButton("+");
        t.ClickButton("−");
        Assert.True(t.HasText("1"));
        Assert.True(t.HasButton("Remettre à zéro"));

        t.ClickButton("Remettre à zéro");
        Assert.True(t.HasText("0"));
        Assert.False(t.HasButton("Remettre à zéro"));
    }

    [Fact]
    public void Typing_then_Enter_adds_a_task_and_clears_the_field()
    {
        using var t = new TestHost(new TodoApp(), themes: DemoThemes.Create());

        t.Click(v => v is TextFieldView);
        t.Type("Publier le POC");
        Assert.Contains(t.Views, v => v is TextFieldView { Value: "Publier le POC" });

        t.Press(KeyCode.Enter);
        Assert.True(t.HasText("Publier le POC"));
        Assert.Contains(t.Views, v => v is TextFieldView { Value: "" });
        Assert.True(t.HasText("2 restante(s)"));
    }

    [Fact]
    public void Backspace_and_arrows_edit_at_the_caret()
    {
        using var t = new TestHost(new TodoApp(), themes: DemoThemes.Create());

        t.Click(v => v is TextFieldView);
        t.Type("abd");
        t.Press(KeyCode.Left);
        t.Type("c");
        t.Press(KeyCode.End);
        t.Press(KeyCode.Backspace);
        Assert.Contains(t.Views, v => v is TextFieldView { Value: "abc" });
    }

    [Fact]
    public void Tapping_a_row_toggles_the_task()
    {
        using var t = new TestHost(new TodoApp(), themes: DemoThemes.Create());

        t.ClickText("Ajouter un backend mobile");
        Assert.True(t.HasText("0 restante(s)"));
        Assert.All(t.Views.OfType<CheckboxView>(), c => Assert.True(c.IsChecked));

        t.ClickText("Ajouter un backend mobile");
        Assert.True(t.HasText("1 restante(s)"));
    }

    [Fact]
    public void Async_action_shows_progress_then_result()
    {
        using var t = new TestHost(new TodoApp(), themes: DemoThemes.Create());

        t.ClickButton("Idée");
        Assert.True(t.HasButton("…"));

        t.WaitUntil(() => t.HasButton("Idée"));
        Assert.True(t.HasText("2 restante(s)"));
    }

    [Fact]
    public void Filters_and_purge()
    {
        using var t = new TestHost(new TodoApp(), themes: DemoThemes.Create());

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

    [Fact]
    public void Changing_theme_keeps_child_component_state()
    {
        using var t = new TestHost(new TodoApp(), themes: DemoThemes.Create());
        t.ClickButton("+");

        t.ClickButton("Sombre");
        Assert.Equal(Theme.Dark.Palette.Page, t.Ui.ClearColor);
        Assert.True(t.HasText("1"));

        t.ClickButton("+");
        Assert.True(t.HasText("2"));
    }
}
