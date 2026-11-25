// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

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
