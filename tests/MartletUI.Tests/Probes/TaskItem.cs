// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A model object with observed state: its bindings are reachable from outside (<c>item.Bind.Done</c>).
/// </summary>
public sealed partial class TaskItem(string title)
{
    public string Title { get; } = title;

    [State] public partial bool Done { get; set; }
}
