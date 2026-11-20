namespace Avalonia.UI.Reactor.Primitives;

public readonly record struct Point(float X, float Y)
{
    public static Point operator *(Point p, float s) => new(p.X * s, p.Y * s);
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
}
