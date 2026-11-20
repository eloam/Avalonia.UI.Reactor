namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Déclare un état observé : <c>[State] partial int Count { get; set; }</c>.
/// Le générateur de code écrit le corps de la propriété : lire l'état depuis un <c>Body</c>
/// abonne ce composant, le modifier (depuis n'importe où, n'importe quel thread)
/// redessine uniquement les composants qui l'ont lu. La classe doit être <c>partial</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class StateAttribute : Attribute;
