// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;
using static MartletUI.Views.Ui;

namespace MartletUI.Tests.Probes;

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
