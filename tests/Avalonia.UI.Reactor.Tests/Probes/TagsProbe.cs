// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A list of values edited in place: one field per tag, bound to its item.
/// </summary>
public sealed class TagsProbe : Component
{
    public StateList<string> Tags { get; } = ["rouge", "vert"];

    public override View Body => VStack(() =>
    {
        Text("Étiquettes");
        ForEach(Tags.Bind, tag => TextField(tag));
        Text(string.Join(", ", Tags));
    });
}
