namespace Avalonia.UI.Reactor.Tests;

using Avalonia.UI.Reactor.Primitives;

/// <summary>Couleurs écrites comme en CSS : <c>"#RRGGBB"</c> ou <c>"#RRGGBBAA"</c>.</summary>
public class ColorTests
{
    [Fact]
    public void Hex_reads_opaque_and_translucent_colors()
    {
        Assert.Equal(new Color(0x25, 0x63, 0xEB), Color.Hex("#2563EB"));
        Assert.Equal(new Color(0x25, 0x63, 0xEB, 0x80), Color.Hex("#2563EB80"));
    }

    [Fact]
    public void An_invalid_length_says_which_formats_are_accepted()
    {
        var error = Assert.Throws<FormatException>(() => Color.Hex("#12345"));

        Assert.Equal("Invalid color '#12345'. Use '#RRGGBB' or '#RRGGBBAA'.", error.Message);
    }
}
