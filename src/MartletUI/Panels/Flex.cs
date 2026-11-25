// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;

namespace MartletUI.Panels;

/// <summary>
/// Provides the attached properties that describe the flexibility of a child in its container (<see cref="FlexStack"/>,
/// <see cref="LayerStack"/>): share of the remaining space, filling and own alignment.
/// </summary>
/// <remarks>
/// The reconciler sets these properties from the view.
/// </remarks>
internal sealed class Flex
{
    /// <summary>
    /// Identifies the share of the remaining space given to a child (<c>.Grow()</c>; 1 for <c>Spacer()</c>).
    /// </summary>
    public static readonly AttachedProperty<double> GrowProperty =
        AvaloniaProperty.RegisterAttached<Flex, Control, double>("Grow");

    /// <summary>
    /// Identifies whether a child takes all the available width.
    /// </summary>
    public static readonly AttachedProperty<bool> FillWidthProperty =
        AvaloniaProperty.RegisterAttached<Flex, Control, bool>("FillWidth");

    /// <summary>
    /// Identifies whether a child takes all the available height.
    /// </summary>
    public static readonly AttachedProperty<bool> FillHeightProperty =
        AvaloniaProperty.RegisterAttached<Flex, Control, bool>("FillHeight");

    /// <summary>
    /// Identifies the own alignment of a child (<c>.AlignSelf(…)</c>); <see langword="null"/> for that of the container.
    /// </summary>
    public static readonly AttachedProperty<Primitives.Align?> SelfAlignProperty =
        AvaloniaProperty.RegisterAttached<Flex, Control, Primitives.Align?>("SelfAlign");

    private Flex()
    {
    }

    /// <summary>
    /// Gets the properties that change the place of a child: a container declares them to lay out again.
    /// </summary>
    public static AvaloniaProperty[] ChildProperties => [GrowProperty, FillWidthProperty, FillHeightProperty, SelfAlignProperty];

    /// <summary>
    /// Gets the share of the remaining space given to a child.
    /// </summary>
    /// <param name="child">The child.</param>
    /// <returns>The share of the remaining space.</returns>
    public static double GetGrow(Control child) => child.GetValue(GrowProperty);

    /// <summary>
    /// Sets the share of the remaining space given to a child.
    /// </summary>
    /// <param name="child">The child.</param>
    /// <param name="value">The share of the remaining space.</param>
    public static void SetGrow(Control child, double value) => child.SetValue(GrowProperty, value);

    /// <summary>
    /// Gets a value indicating whether a child takes all the available width.
    /// </summary>
    /// <param name="child">The child.</param>
    /// <returns><see langword="true"/> if the child fills the width.</returns>
    public static bool GetFillWidth(Control child) => child.GetValue(FillWidthProperty);

    /// <summary>
    /// Sets a value indicating whether a child takes all the available width.
    /// </summary>
    /// <param name="child">The child.</param>
    /// <param name="value"><see langword="true"/> if the child fills the width.</param>
    public static void SetFillWidth(Control child, bool value) => child.SetValue(FillWidthProperty, value);

    /// <summary>
    /// Gets a value indicating whether a child takes all the available height.
    /// </summary>
    /// <param name="child">The child.</param>
    /// <returns><see langword="true"/> if the child fills the height.</returns>
    public static bool GetFillHeight(Control child) => child.GetValue(FillHeightProperty);

    /// <summary>
    /// Sets a value indicating whether a child takes all the available height.
    /// </summary>
    /// <param name="child">The child.</param>
    /// <param name="value"><see langword="true"/> if the child fills the height.</param>
    public static void SetFillHeight(Control child, bool value) => child.SetValue(FillHeightProperty, value);

    /// <summary>
    /// Gets the own alignment of a child.
    /// </summary>
    /// <param name="child">The child.</param>
    /// <returns>The alignment, or <see langword="null"/> for that of the container.</returns>
    public static Primitives.Align? GetSelfAlign(Control child) => child.GetValue(SelfAlignProperty);

    /// <summary>
    /// Sets the own alignment of a child.
    /// </summary>
    /// <param name="child">The child.</param>
    /// <param name="value">The alignment, or <see langword="null"/> for that of the container.</param>
    public static void SetSelfAlign(Control child, Primitives.Align? value) => child.SetValue(SelfAlignProperty, value);

    /// <summary>
    /// Places a child on one axis: stretched if it fills or if the alignment asks for it, otherwise at its natural size
    /// (without overflowing), at the start, the center or the end of the space offered.
    /// </summary>
    /// <param name="available">The space offered on the axis.</param>
    /// <param name="natural">The natural size of the child on the axis.</param>
    /// <param name="fill">A value indicating whether the child fills the axis.</param>
    /// <param name="align">The alignment of the child.</param>
    /// <returns>The position and the size of the child on the axis.</returns>
    public static (double Position, double Size) Place(double available, double natural, bool fill, Primitives.Align align)
    {
        double size = fill || align == Primitives.Align.Stretch ? available : Math.Min(natural, available);
        double position = align switch
        {
            Primitives.Align.Center => (available - size) / 2,
            Primitives.Align.End => available - size,
            _ => 0,
        };
        return (position, size);
    }
}
