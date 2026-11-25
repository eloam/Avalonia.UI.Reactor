// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using MartletUI.State;
using MartletUI.Tests.Probes;

namespace MartletUI.Tests;

/// <summary>
/// A child keeps its control when a sibling appears before it: identified by its key, by an identity it already has (a
/// component, a bound control), otherwise by its type and its rank among the children of that type.
/// </summary>
public class IdentityTests
{
    [AvaloniaFact]
    public void A_field_keeps_its_control_focus_and_caret_when_a_view_of_another_type_appears_before_it()
    {
        var probe = new ErrorFieldProbe();
        using var t = new MartletTestHost(probe);
        TextBox field = t.Field;
        t.Click(field);
        t.Type("abc");

        probe.HasError = true;
        t.Settle();

        Assert.True(t.HasText("Adresse invalide"));
        Assert.Same(field, t.Field);
        Assert.True(field.IsFocused);
        Assert.Equal(3, field.CaretIndex);
    }

    [AvaloniaFact]
    public void A_bound_field_keeps_its_control_and_focus_when_a_field_of_the_same_type_appears_before_it()
    {
        var probe = new CompanyFormProbe();
        using var t = new MartletTestHost(probe);
        TextBox email = t.Field;
        t.Click(email);
        t.Type("abc");

        probe.IsCompany = true;
        t.Settle();

        TextBox[] fields = [.. t.Controls.OfType<TextBox>()];
        Assert.Equal(2, fields.Length);
        Assert.Same(email, fields[1]);
        Assert.True(email.IsFocused);
        Assert.Equal("abc", email.Text);
    }

    [AvaloniaFact]
    public void Two_fields_bound_to_the_same_property_fall_back_to_their_rank()
    {
        var probe = new CompanyFormProbe(bindEmailTwice: true);
        using var t = new MartletTestHost(probe);

        Assert.Equal(2, t.Controls.OfType<TextBox>().Count());
        Assert.Empty(t.Errors);
    }

    [Fact]
    public void A_binding_to_a_property_needs_its_owner_and_its_name()
    {
        Assert.Throws<ArgumentNullException>(() => new Binding<int>(() => 0, _ => { }, null!, "Count"));
        Assert.Throws<ArgumentException>(() => new Binding<int>(() => 0, _ => { }, new object(), ""));
    }

    [AvaloniaFact]
    public void A_component_keeps_its_controls_when_a_component_of_the_same_type_appears_before_it()
    {
        var probe = new OptionalCounterProbe();
        using var t = new MartletTestHost(probe);
        Button increment = t.ButtonLabeled("b+");

        probe.ShowFirst = true;
        t.Settle();

        Assert.True(t.HasButton("a+"));
        Assert.Same(increment, t.ButtonLabeled("b+"));
    }
}
