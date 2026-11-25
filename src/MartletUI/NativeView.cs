// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using MartletUI.Views;

namespace MartletUI;

/// <summary>
/// Represents an Avalonia control placed in a view with <see cref="AvaloniaViews.Native{T}"/>.
/// </summary>
/// <remarks>
/// The control is created once at its place, then updated on each new version of the view. Modifiers apply to it as to
/// any view.
/// </remarks>
public sealed record NativeView : View
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NativeView"/> class.
    /// </summary>
    /// <param name="controlType">The type of the control.</param>
    /// <param name="create">The function that creates the control.</param>
    /// <param name="update">The function that updates the control, or <see langword="null"/>.</param>
    internal NativeView(Type controlType, Func<Control> create, Action<Control>? update)
    {
        ControlType = controlType;
        Create = create;
        Update = update;
    }

    /// <summary>
    /// Gets the type of the control; another type at the same place creates a new control.
    /// </summary>
    internal Type ControlType { get; }

    /// <summary>
    /// Gets the function that creates the control.
    /// </summary>
    internal Func<Control> Create { get; }

    /// <summary>
    /// Gets the function that updates the control from the state, or <see langword="null"/>.
    /// </summary>
    internal Action<Control>? Update { get; }
}
