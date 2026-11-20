namespace Mvu.Views;

/// <summary>
/// Zone recalculée indépendamment : un composant, un élément de <c>ForEach</c> ou un bloc <c>Observe</c>.
/// Seuls les états lus dans <see cref="Build"/> la font recalculer. Les modificateurs se placent sur son contenu.
/// </summary>
public sealed record ScopeView(Func<View> Build) : View
{
    /// <summary>Instance de composant : son état survit aux recalculs du parent.</summary>
    internal object? Identity { get; init; }
}
