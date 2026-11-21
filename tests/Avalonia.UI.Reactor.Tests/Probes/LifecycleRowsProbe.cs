// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// Keyed rows that record when they appear and disappear.
/// </summary>
public sealed class LifecycleRowsProbe : Component
{
    public StateList<string> Items { get; } = ["a", "b", "c"];

    public List<string> Appeared { get; } = [];

    public List<string> Disappeared { get; } = [];

    public override View Body => VStack(ForEach(Items, Row));

    private View Row(string item) => Text(item).OnAppear(() => Appeared.Add(item)).OnDisappear(() => Disappeared.Add(item));
}
