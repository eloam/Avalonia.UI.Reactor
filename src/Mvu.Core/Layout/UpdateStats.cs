namespace Mvu.Layout;

/// <summary>Bilan de la dernière mise à jour (tests, outils de debug).</summary>
public readonly record struct UpdateStats(int Evaluated, int Reused, int Scopes);
