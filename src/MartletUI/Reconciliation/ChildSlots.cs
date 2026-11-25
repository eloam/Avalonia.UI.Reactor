// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Globalization;
using MartletUI.Views;

namespace MartletUI.Reconciliation;

/// <summary>
/// Gives the children of a container their places: the name under which each child finds the node that held it at the
/// previous update, so that it keeps its control, focus and hover when a sibling appears, disappears or moves.
/// </summary>
/// <remarks>
/// The place of a child comes from, in this order:
/// <list type="number">
/// <item><description>its <c>Key</c>: <c>parent/kdetails</c>;</description></item>
/// <item><description>an identity it already has, a component by its instance, a view method by its name, a bound
/// control by the property of its binding: <c>parent/o#7</c>, <c>parent/o#3.Email</c>;</description></item>
/// <item><description>otherwise its view type and its rank among the children of that type that the first two rules did
/// not place: <c>parent/tTextView#0</c>, so that a view of another type that appears before it does not shift
/// it.</description></item>
/// </list>
/// Each rule has its own prefix, so a key that looks like a rank (a row keyed by the id 0) never takes the place of
/// another child. Two children that share an identity (the same view method called twice, two fields bound to the same
/// property) fall back to their rank; two children that share a key are an error.
/// </remarks>
/// <param name="parentId">The path of the container.</param>
internal sealed class ChildSlots(string parentId)
{
    private const string KeyPrefix = "k";
    private const string IdentityPrefix = "o";
    private const string RankPrefix = "t";

    // Separates the type of a view from its rank, and the owner of a bound property from its name.
    private const char RankSeparator = '#';
    private const char PropertySeparator = '.';

    private readonly HashSet<string> _taken = [];
    private readonly Dictionary<Type, int> _ranks = [];

    /// <summary>
    /// Returns the place of the next child of the container.
    /// </summary>
    /// <param name="view">The view of the child.</param>
    /// <returns>The path of the place of the child.</returns>
    /// <exception cref="InvalidOperationException">Another child of the container has the same key.</exception>
    public string Of(View view)
    {
        if (view.Key is { } key)
        {
            string keyed = $"{parentId}/{KeyPrefix}{key}";
            if (!_taken.Add(keyed))
            {
                throw new InvalidOperationException(
                    $"Two children of the same container have the key '{key}'. Give each child a unique key.");
            }

            return keyed;
        }

        if (IdentityOf(view) is { } identity)
        {
            string identified = $"{parentId}/{IdentityPrefix}{identity}";
            if (_taken.Add(identified))
            {
                return identified;
            }
        }

        Type type = view.GetType();
        _ranks.TryGetValue(type, out int rank);
        _ranks[type] = rank + 1;
        string ranked = $"{parentId}/{RankPrefix}{type.Name}{RankSeparator}{rank.ToString(CultureInfo.InvariantCulture)}";
        _taken.Add(ranked);
        return ranked;
    }

    // A component by its instance, a view method by its name, a bound control by the property of its binding.
    private static string? IdentityOf(View view) => view switch
    {
        ScopeView { Identity: { } identity } => ReferenceKeys.Of(identity),
        { BindingTarget: { } target } => $"{ReferenceKeys.Of(target.Owner)}{PropertySeparator}{target.Property}",
        _ => null,
    };
}
