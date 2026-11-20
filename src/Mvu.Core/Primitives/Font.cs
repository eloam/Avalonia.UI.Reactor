namespace Mvu.Primitives;

/// <summary>Police résolue. Weight : 100 (fin) à 900 (noir), 400 = normal.</summary>
public readonly record struct Font(string Family, float Size, int Weight)
{
    public bool IsBold => Weight >= FontWeight.SemiBold;
}
