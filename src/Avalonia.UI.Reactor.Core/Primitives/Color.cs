using System.Globalization;

namespace Avalonia.UI.Reactor.Primitives;

public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    public static readonly Color Transparent = new(0, 0, 0, 0);
    public static readonly Color White = new(255, 255, 255);
    public static readonly Color Black = new(0, 0, 0);

    /// <summary>"#RRGGBB" ou "#RRGGBBAA".</summary>
    public static Color Hex(string hex)
    {
        var s = hex.TrimStart('#');
        uint v = uint.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return s.Length switch
        {
            6 => new((byte)(v >> 16), (byte)(v >> 8), (byte)v),
            8 => new((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v),
            _ => throw new FormatException($"Couleur invalide : {hex}")
        };
    }

    /// <summary>Permet d'écrire <c>.Background("#2563EB")</c>.</summary>
    public static implicit operator Color(string hex) => Hex(hex);

    public Color WithAlpha(float opacity) => this with { A = (byte)Math.Clamp(opacity * 255f, 0f, 255f) };
    public Color Mix(Color o, float t) => new(L(R, o.R, t), L(G, o.G, t), L(B, o.B, t), L(A, o.A, t));
    public Color Darken(float t) => Mix(Black with { A = A }, t);
    public Color Lighten(float t) => Mix(White with { A = A }, t);
    static byte L(byte a, byte b, float t) => (byte)Math.Round(a + (b - a) * t);
}
