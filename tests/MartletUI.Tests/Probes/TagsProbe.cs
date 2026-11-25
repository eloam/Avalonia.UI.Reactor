// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

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
