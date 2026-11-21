// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Tests;

/// <summary>
/// Keyboard shortcuts written as they read.
/// </summary>
public class KeyGestureTests
{
    [Theory]
    [InlineData("Ctrl+N", KeyCode.N, KeyModifiers.Ctrl)]
    [InlineData("ctrl+shift+s", KeyCode.S, KeyModifiers.Ctrl | KeyModifiers.Shift)]
    [InlineData("Alt+F4", KeyCode.F4, KeyModifiers.Alt)]
    [InlineData("F5", KeyCode.F5, KeyModifiers.None)]
    [InlineData("Ctrl++", KeyCode.Plus, KeyModifiers.Ctrl)]
    [InlineData("Ctrl+1", KeyCode.D1, KeyModifiers.Ctrl)]
    [InlineData("Shift+Del", KeyCode.Delete, KeyModifiers.Shift)]
    public void Parse_reads_modifiers_and_key(string text, KeyCode key, KeyModifiers modifiers) =>
        Assert.Equal(new KeyGesture(key, modifiers), KeyGesture.Parse(text));

    [Theory]
    [InlineData("Ctrl+")]
    [InlineData("Hyper+N")]
    [InlineData("F13")]
    [InlineData("")]
    public void Parse_rejects_an_invalid_shortcut_with_an_explicit_message(string text)
    {
        ArgumentException e = Assert.Throws<ArgumentException>(() => KeyGesture.Parse(text));
        Assert.StartsWith($"'{text}' is not a valid keyboard shortcut. Use a form such as 'Ctrl+N'", e.Message);
    }

    [Fact]
    public void Shortcut_text_follows_the_platform_conventions()
    {
        string text = KeyGesture.Parse("Ctrl+Shift+S").ToString();
        Assert.Equal(OperatingSystem.IsMacOS() ? "⇧⌘S" : "Ctrl+Shift+S", text);
    }

    [Fact]
    public void Matches_requires_exactly_the_same_modifiers()
    {
        var save = KeyGesture.Parse("Ctrl+S");
        KeyModifiers ctrl = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Ctrl;

        Assert.True(save.Matches(KeyCode.S, ctrl));
        Assert.False(save.Matches(KeyCode.S, ctrl | KeyModifiers.Shift));
        Assert.False(save.Matches(KeyCode.N, ctrl));
    }

    [Theory]
    [InlineData("Ctrl+N", true)]
    [InlineData("F5", true)]
    [InlineData("Alt+X", true)]
    [InlineData("P", false)]
    [InlineData("Shift+P", false)]
    public void Only_command_shortcuts_apply_while_typing(string text, bool isCommand) =>
        Assert.Equal(isCommand, KeyGesture.Parse(text).IsCommand);
}
