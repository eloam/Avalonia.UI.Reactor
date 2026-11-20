using System.Runtime.CompilerServices;
using Avalonia.UI.Reactor.Primitives;

namespace Avalonia.UI.Reactor.Views;

/// <summary>
/// Zone recalculée indépendamment : un composant, un élément de <c>ForEach</c> ou un bloc <c>Observe</c>.
/// Seuls les états lus dans <see cref="Build"/> la font recalculer. Les modificateurs se placent sur son contenu.
/// </summary>
public sealed record ScopeView(Func<View> Build) : View
{
    static readonly ConditionalWeakTable<ScopeView, ZStackView> Wrappers = new();

    /// <summary>Instance de composant : son état survit aux recalculs du parent.</summary>
    internal object? Identity { get; init; }

    /// <summary>
    /// Modificateurs posés sur la portée (<c>counter.Padding(8)</c>) : ils s'appliquent à une enveloppe autour de son
    /// contenu, alignée en haut à gauche ; null si elle n'en porte aucun. L'enveloppe est gardée en cache pour que la même
    /// portée donne toujours la même référence (les moteurs comparent les vues par référence).
    /// </summary>
    internal ZStackView? ModifierWrapper()
    {
        var plain = new ScopeView(Build) { Identity = Identity };
        if (this with { Key = null } == plain) return null;
        return Wrappers.GetValue(this, s =>
            new ZStackView([plain]) { Alignment = Alignment.TopLeft }.CopyFrom(s) with { Key = null });
    }
}
