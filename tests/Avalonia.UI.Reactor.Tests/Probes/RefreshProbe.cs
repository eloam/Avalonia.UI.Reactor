// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace Avalonia.UI.Reactor.Tests.Probes;

using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Views;
using static Avalonia.UI.Reactor.Views.Ui;

/// <summary>
/// A component without <c>[State]</c>: a plain field, displayed after an explicit call to <c>Refresh</c>.
/// </summary>
public sealed class RefreshProbe : Component
{
    private int _value;

    public void Set(int value)
    {
        _value = value;
        Refresh();
    }

    public override View Body => Text($"{_value}");
}
