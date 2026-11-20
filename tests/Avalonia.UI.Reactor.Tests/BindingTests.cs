using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>Liaisons à double sens générées pour les états [State] : <c>Bind.Draft</c>, <c>item.Bind.Done</c>.</summary>
public class BindingTests
{
    [Fact]
    public void TextField_bound_to_a_state_reads_and_writes_it()
    {
        var form = new FormProbe();
        using var t = new TestHost(form);

        t.Click(v => v is TextFieldView);
        t.Type("Ada");
        Assert.Equal("Ada", form.Name);

        form.Name = "Grace";            // et dans l'autre sens : l'état modifié ailleurs s'affiche
        t.Ui.Invalidate();
        Assert.Contains(t.Views, v => v is TextFieldView { Value: "Grace" });
    }

    [Fact]
    public void Checkbox_bound_to_a_state_toggles_it()
    {
        var form = new FormProbe();
        using var t = new TestHost(form);

        t.Click(v => v is CheckboxView);
        Assert.True(form.Agreed);
        t.Click(v => v is CheckboxView);
        Assert.False(form.Agreed);
    }

    [Fact]
    public void Binding_works_on_a_model_object()
    {
        var list = new TaskListProbe();
        using var t = new TestHost(list);

        t.Click(v => v is CheckboxView { Label: "Écrire les tests" });
        Assert.True(list.Tasks[0].Done);
        Assert.False(list.Tasks[1].Done);
    }
}
