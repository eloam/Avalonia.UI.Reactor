namespace Avalonia.UI.Reactor.State;

/// <summary>Bilan de la dernière mise à jour (tests, outils de debug).</summary>
internal readonly record struct UpdateStats(int Evaluated, int Reused, int Scopes);
