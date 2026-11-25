// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.


using MartletUI.State;
using MartletUI.Views;

namespace MartletUI.Tests.Probes;

/// <summary>
/// A component without state that displays a given view: to check the layout or the theme of a view.
/// </summary>
public sealed class ViewProbe(Func<View> body) : Component
{
    public override View Body => body();
}
