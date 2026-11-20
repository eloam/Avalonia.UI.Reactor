namespace Avalonia.UI.Reactor.Primitives;

public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public Point Center => new(X + Width / 2, Y + Height / 2);
    /// <summary>Carré centré, du plus petit côté (cercles, jauges).</summary>
    public Rect CenteredSquare(float inset = 0)
    {
        float s = Math.Max(0, Math.Min(Width, Height) - 2 * inset);
        return new(X + (Width - s) / 2, Y + (Height - s) / 2, s, s);
    }
}
