// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace TodoDemo.Components;

/// <summary>
/// Represents a self-contained counter: clicking "+" re-evaluates this card only.
/// </summary>
public sealed partial class CounterCard : Component
{
    /// <inheritdoc/>
    public override View Body =>
        Card(
            Text("Counter").Font(TextStyle.Headline),
            HStack(() =>
            {
                Button("−", () => Count--).Style(ButtonStyle.Secondary);
                Text($"{Count}").FontSize(28).Bold().Width(72).TextAlignment(TextAlign.Center);
                Button("+", () => Count++);
                Spacer();
                if (Count != 0)
                    Button("Reset", () => Count = 0).Style(ButtonStyle.Ghost);
            }));

    [State] private partial int Count { get; set; }
}
