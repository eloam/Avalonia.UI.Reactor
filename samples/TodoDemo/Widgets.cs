// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace TodoDemo;

/// <summary>
/// Reusable components without state: plain functions that return a view.
/// </summary>
public static class Widgets
{
    /// <summary>
    /// A card: always the full width, even when its content does not stretch.
    /// </summary>
    public static StackView Card(params View?[] children) =>
        VStack(children)
            .Spacing(12)
            .Padding(16)
            .Background(ColorRole.Surface)
            .Border(ColorRole.Border)
            .Radius(10)
            .FillWidth();

    /// <summary>
    /// A custom control: a ring drawn with Canvas, with the percentage layered over it with ZStack.
    /// </summary>
    public static View ProgressRing(int done, int total, float size = 40)
    {
        float progress = total == 0 ? 0 : (float)done / total;
        return ZStack(
            Canvas(d =>
            {
                const float thickness = 4;
                Point center = d.Bounds.Center;
                float radius = d.Bounds.CenteredSquare().Width / 2 - thickness / 2;
                d.StrokeCircle(center, radius, ColorRole.Border, thickness);
                d.Arc(center, radius, -90, 360 * progress, progress >= 1 ? "success" : ColorRole.Accent, thickness);
            }).Size(size, size),
            Text($"{Math.Round(progress * 100)}%").FontSize(size / 4).Bold());
    }

    /// <summary>
    /// An exclusive choice among a few options, bound to state: <c>Segmented(Bind.Filter, ("All", Filter.All), …)</c>.
    /// </summary>
    public static View Segmented<T>(Binding<T> value, params (string Label, T Value)[] options)
    {
        T? selected = value.Value;
        return HStack([.. options.Select(o =>
                Button(o.Label, () => value.Set(o.Value))
                    .Style(Equals(o.Value, selected) ? ButtonStyle.Secondary : ButtonStyle.Ghost)
                    .Padding(10, 4)
                    .Font(TextStyle.Small))])
            .Spacing(4)
            .Padding(3)
            .Background(ColorRole.Surface)
            .Border(ColorRole.Border)
            .Radius(8);
    }
}
