using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;
using Avalonia.UI.Reactor.Widgets;

/// <summary><c>Native(…)</c> : le contrôle de l'application, dans un <see cref="NativeHost"/> qui suit sa mise à jour.</summary>
internal sealed class NativeAdapter : ViewAdapter
{
    public static readonly NativeAdapter Instance = new();

    public override Control Create(Reconciler reconciler, Node node) =>
        new NativeHost { Child = ((NativeView)node.View).Create() };

    public override void Update(Reconciler reconciler, Node node, View view, ViewEnvironment env)
    {
        var native = (NativeView)view;
        var host = (NativeHost)node.Content;
        if (native.Update is { } update && host.Child is { } control) host.Apply(() => update(control));
    }

    /// <summary>Le contrôle est repris tant que la vue en demande un du même type.</summary>
    public override bool CanReuse(Node existing, View view) =>
        existing.View is NativeView previous && previous.ControlType == ((NativeView)view).ControlType;
}
