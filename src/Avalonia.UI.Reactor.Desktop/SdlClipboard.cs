using Avalonia.UI.Reactor.Runtime;
using static SDL.SDL3;

namespace Avalonia.UI.Reactor.Desktop;

/// <summary>Presse-papiers du système, par SDL (Windows, macOS, Linux).</summary>
internal sealed class SdlClipboard : IClipboard
{
    public string? GetText() => SDL_HasClipboardText() ? SDL_GetClipboardText() : null;

    public void SetText(string text)
    {
        if (!SDL_SetClipboardText(text)) Console.Error.WriteLine($"[Reactor] Failed to set the clipboard text: {SDL_GetError()}");
    }
}
