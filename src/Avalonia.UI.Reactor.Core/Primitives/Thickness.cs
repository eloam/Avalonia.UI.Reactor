namespace Avalonia.UI.Reactor.Primitives;

public readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
{
    public Thickness(float all) : this(all, all, all, all) { }
    public Thickness(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }
    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;
}
