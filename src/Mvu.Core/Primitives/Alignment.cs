namespace Mvu.Primitives;

/// <summary>Position dans un rectangle, sur les deux axes (ZStack, Overlay).</summary>
public readonly record struct Alignment(Align Horizontal, Align Vertical)
{
    public static readonly Alignment TopLeft = new(Align.Start, Align.Start);
    public static readonly Alignment Top = new(Align.Center, Align.Start);
    public static readonly Alignment TopRight = new(Align.End, Align.Start);
    public static readonly Alignment Left = new(Align.Start, Align.Center);
    public static readonly Alignment Center = new(Align.Center, Align.Center);
    public static readonly Alignment Right = new(Align.End, Align.Center);
    public static readonly Alignment BottomLeft = new(Align.Start, Align.End);
    public static readonly Alignment Bottom = new(Align.Center, Align.End);
    public static readonly Alignment BottomRight = new(Align.End, Align.End);
    public static readonly Alignment Fill = new(Align.Stretch, Align.Stretch);
}
