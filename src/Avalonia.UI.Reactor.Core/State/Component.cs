using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Un écran ou un morceau d'écran : de l'état (<c>[State]</c>) et un <see cref="Body"/>.
/// <code>
/// partial class Counter : Component
/// {
///     [State] partial int Count { get; set; }
///     public override View Body => Button($"{Count}", () => Count++);
/// }
/// </code>
/// Un composant s'insère dans la vue d'un autre comme n'importe quelle vue (<c>VStack(counter, …)</c>)
/// et n'est recalculé que si un état qu'il a lu change.
/// </summary>
public abstract class Component
{
    readonly Signal _refresh = new();
    ScopeView? _view;

    public abstract View Body { get; }

    /// <summary>Fenêtre qui affiche ce composant (renseignée à son premier affichage).</summary>
    internal UiHost? Host { get; set; }

    /// <summary>
    /// Catalogue des thèmes de l'application, par exemple pour construire un sélecteur avec <c>Themes.Families</c>.
    /// Pour habiller une vue, c'est la vue qui le déclare : <c>.Theme("Sable")</c>, <c>.ColorScheme(ColorScheme.Dark)</c>.
    /// Disponible dès que le composant est affiché (dans <see cref="Body"/> et dans les actions).
    /// </summary>
    protected Themes Themes => Host?.Themes
        ?? throw new InvalidOperationException("'Themes' is available only after the component has been displayed.");

    /// <summary>
    /// Force le recalcul de ce composant. Inutile avec <c>[State]</c> et <see cref="StateList{T}"/> ;
    /// sert pour un état non observé (champ ordinaire, objet externe). Appelable depuis n'importe quel thread.
    /// </summary>
    protected void Refresh() => _refresh.Notify();

    View Render()
    {
        _refresh.Read();
        return Body;
    }

    /// <summary>Un composant se place comme une vue ; null reste null (<c>ShowCounter ? counter : null</c>).</summary>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(c))]
    public static implicit operator View?(Component? c) => c is null ? null : c._view ??= new ScopeView(c.Render) { Identity = c };
}
