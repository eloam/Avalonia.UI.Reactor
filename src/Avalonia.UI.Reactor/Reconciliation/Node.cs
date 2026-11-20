using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Nœud vivant du réconciliateur : le contrôle Avalonia qui affiche une vue. Il est repris d'une mise à jour à l'autre
/// tant que la vue garde le même chemin et la même sorte de contrôle ; seules ses propriétés changent alors.
/// </summary>
internal sealed class Node
{
    public Node(string id, ViewAdapter adapter, View view, Func<Node, Control> create)
    {
        Id = id;
        Slot = id;
        Adapter = adapter;
        View = view;
        Content = create(this);
    }

    /// <summary>Chemin de la vue dans l'arbre, comme celui de l'ancien moteur : <c>r.</c>, <c>r./0</c>, <c>r./todo-3</c>…</summary>
    public string Id { get; }

    /// <summary>Place où le parent a posé ce nœud ; diffère de <see cref="Id"/> pour le contenu d'une portée (<c>…/0</c> et <c>…/0.</c>).</summary>
    public string Slot { get; set; }

    public ViewAdapter Adapter { get; }

    /// <summary>Dernière vue appliquée : les événements du contrôle (clic…) lisent son action.</summary>
    public View View { get; set; }

    /// <summary>Environnement de la vue (thème de sa branche, police et couleur héritées).</summary>
    public ViewEnvironment Environment { get; set; }

    /// <summary>Contrôle qui affiche la vue.</summary>
    public Control Content { get; }

    /// <summary>Enveloppe qui dessine fond, bordure, angles et marge intérieure quand le contrôle ne sait pas le faire.</summary>
    public Border? Decoration { get; set; }

    /// <summary>Contrôle placé dans le parent : l'enveloppe s'il y en a une, sinon le contrôle.</summary>
    public Control Root => Decoration ?? Content;

    public IReadOnlyList<Node> Children { get; set; } = [];

    /// <summary>Prend toute la largeur disponible (<c>.FillWidth()</c>, ou une pile dont un enfant le fait).</summary>
    public bool FillWidth { get; set; }

    public bool FillHeight { get; set; }

    /// <summary>Part de l'espace restant sur l'axe de la pile parente.</summary>
    public float Grow { get; set; }
}
