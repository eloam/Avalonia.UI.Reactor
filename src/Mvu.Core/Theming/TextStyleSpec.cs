using Mvu.Primitives;
namespace Mvu.Theming;

/// <summary>Taille et graisse d'un style de texte (graisse : 100 fin → 900 noir, 400 normal).</summary>
public sealed record TextStyleSpec(float Size, int Weight = FontWeight.Regular);
