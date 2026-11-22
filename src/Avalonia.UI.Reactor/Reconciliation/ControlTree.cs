// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Collections;
using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

/// <summary>
/// Moves Avalonia controls in the tree of controls with as few changes as possible, so that they keep their focus and
/// hover.
/// </summary>
internal static class ControlTree
{
    /// <summary>
    /// Removes a control from its current parent, before placing it elsewhere.
    /// </summary>
    /// <param name="control">The control to remove.</param>
    public static void Detach(Control control)
    {
        switch (control.Parent)
        {
            case Panel panel:
                panel.Children.Remove(control);
                break;
            case Decorator decorator:
                decorator.Child = null;
                break;
        }
    }

    /// <summary>
    /// Arranges the controls of the children in a container: a control already present is moved, not removed and
    /// inserted again.
    /// </summary>
    /// <param name="children">The children of the container.</param>
    /// <param name="nodes">The nodes of the children, in order.</param>
    public static void Arrange(IAvaloniaList<Control> children, IReadOnlyList<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Control wanted = nodes[i].Root;
            if (i < children.Count && ReferenceEquals(children[i], wanted))
            {
                continue;
            }

            int index = children.IndexOf(wanted);
            if (index > i)
            {
                children.Move(index, i);
            }
            else
            {
                Detach(wanted);
                children.Insert(i, wanted);
            }
        }

        while (children.Count > nodes.Count)
        {
            children.RemoveAt(children.Count - 1);
        }
    }
}
