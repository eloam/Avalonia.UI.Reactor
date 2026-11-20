using Mvu.Runtime;
using static SDL.SDL3;

namespace Mvu.Desktop;

/// <summary>Presse-papiers du système, par SDL (Windows, macOS, Linux).</summary>
internal sealed class SdlClipboard : IClipboard
{
    public string? GetText() => SDL_HasClipboardText() ? SDL_GetClipboardText() : null;

    public void SetText(string text)
    {
        if (!SDL_SetClipboardText(text)) Console.Error.WriteLine($"[Mvu] Failed to set the clipboard text: {SDL_GetError()}");
    }
}
