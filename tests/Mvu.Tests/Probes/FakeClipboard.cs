using Mvu.Runtime;

namespace Mvu.Tests.Probes;

/// <summary>Presse-papiers en mémoire, à la place de celui du système.</summary>
public sealed class FakeClipboard(string? text = null) : IClipboard
{
    public string? Text { get; private set; } = text;

    public string? GetText() => Text;

    public void SetText(string text) => Text = text;
}
