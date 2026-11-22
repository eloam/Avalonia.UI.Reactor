// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace TodoDemo;

/// <summary>
/// Provides reusable pieces of view without state: plain functions that return a view.
/// </summary>
public static class Widgets
{
    /// <summary>
    /// Creates a card: always the full width, even when its content does not stretch.
    /// </summary>
    /// <param name="children">The views of the card, from top to bottom.</param>
    /// <returns>The card.</returns>
    public static StackView Card(params View?[] children) => Decorate(VStack(children));

    /// <summary>
    /// Creates a card whose content is a block, as with the containers of the framework.
    /// </summary>
    /// <example>
    /// <code>
    /// Card(() =&gt;
    /// {
    ///     Title();
    ///     if (HasMore)
    ///         Details();
    /// })
    /// </code>
    /// </example>
    /// <param name="content">The block, whose views are those of the card.</param>
    /// <returns>The card.</returns>
    public static StackView Card([ViewBuilder] Action content) => Decorate(VStack(content));

    /// <summary>
    /// Creates a custom control: a ring drawn with <c>Canvas</c>, with the percentage layered over it with <c>ZStack</c>.
    /// </summary>
    /// <param name="done">The number of completed items.</param>
    /// <param name="total">The number of items.</param>
    /// <param name="size">The width and height of the ring.</param>
    /// <returns>The progress ring.</returns>
    public static View ProgressRing(int done, int total, float size = 40)
    {
        float progress = total == 0 ? 0 : (float)done / total;
        return ZStack(
            Canvas(drawing =>
            {
                const float thickness = 4;
                Point center = drawing.Bounds.Center;
                float radius = (drawing.Bounds.CenteredSquare().Width / 2) - (thickness / 2);
                drawing.StrokeCircle(center, radius, ColorRole.Border, thickness);
                drawing.Arc(center, radius, -90, 360 * progress, progress >= 1 ? DemoThemes.Success : ColorRole.Accent, thickness);
            }).Size(size, size),
            Text($"{Math.Round(progress * 100)}%").FontSize(size / 4).Bold());
    }

    /// <summary>
    /// Creates an exclusive choice among a few options, bound to state.
    /// </summary>
    /// <example>
    /// <code>
    /// Segmented(Bind.Filter, ("All", Filter.All), ("Active", Filter.Active), ("Done", Filter.Done))
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The binding to the chosen value.</param>
    /// <param name="options">The label and the value of each option.</param>
    /// <returns>The choice.</returns>
    public static View Segmented<T>(Binding<T> value, params (string Label, T Value)[] options)
    {
        T? selected = value.Value;
        return HStack([.. options.Select(option =>
                Button(option.Label, () => value.Set(option.Value))
                    .Style(Equals(option.Value, selected) ? ButtonStyle.Secondary : ButtonStyle.Ghost)
                    .Padding(10, 4)
                    .Font(TextStyle.Small))])
            .Spacing(4)
            .Padding(3)
            .Background(ColorRole.Surface)
            .Border(ColorRole.Border)
            .Radius(8);
    }

    private static StackView Decorate(StackView content) =>
        content
            .Spacing(12)
            .Padding(16)
            .Background(ColorRole.Surface)
            .Border(ColorRole.Border)
            .Radius(10)
            .FillWidth();
}
