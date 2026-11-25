// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Headless.XUnit;
using MartletUI.Runtime;
using MartletUI.State;
using MartletUI.Tests.Probes;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests;

/// <summary>
/// .NET hot reload re-evaluates every scope of an Avalonia window without losing the state.
/// </summary>
public class HotReloadTests
{
    [AvaloniaFact]
    public void Hot_reload_reevaluates_every_component_and_keeps_their_state()
    {
        using var t = new MartletTestHost(new TwoCountersProbe());
        t.ClickButton("a+");

        HotReload.UpdateApplication(null);
        t.Settle();

        Assert.Equal(new UpdateStats(Evaluated: 3, Reused: 0, Scopes: 3), t.Stats);
        Assert.True(t.HasText("a1"));
    }

    [AvaloniaFact]
    public void A_state_property_added_by_hot_reload_works_on_the_objects_that_already_exist()
    {
        // Hot reload adds a property to an existing object without running its initializers, as here.
        var task = (TaskItem)RuntimeHelpers.GetUninitializedObject(typeof(TaskItem));
        using var t = new MartletTestHost(new ViewProbe(() => Text($"done: {task.Done}")));
        Assert.Empty(t.Errors);

        task.Done = true;
        t.Settle();

        Assert.True(t.HasText("done: True"));
    }

    [Fact]
    public void The_signal_of_a_state_property_is_created_with_its_object()
    {
        // Without an initializer in the generated code, hot reload adds a property without updating the constructors, so
        // that new objects miss its initial value (checked with 'dotnet watch', .NET 10).
        FieldInfo? signal = typeof(TaskItem).GetField("__DoneSignalStorage", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(signal?.GetValue(new TaskItem("title")));
    }
}
