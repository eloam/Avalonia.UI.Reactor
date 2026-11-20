using Avalonia.Controls;

namespace Avalonia.UI.Reactor.Reconciliation;

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

/// <summary>
/// Sorte de contrôle qui affiche un type de vue : il crée le contrôle une fois, puis lui applique chaque nouvelle
/// version de la vue. Un adaptateur n'a pas d'état : il travaille sur le nœud qu'on lui passe.
/// </summary>
internal abstract class ViewAdapter
{
    /// <summary>Crée le contrôle d'un nœud (une seule fois par nœud).</summary>
    public abstract Control Create(Reconciler reconciler, Node node);

    /// <summary>Applique la vue au contrôle du nœud : propriétés, puis enfants.</summary>
    public abstract void Update(Reconciler reconciler, Node node, View view, ViewEnvironment env);

    /// <summary>Le contrôle dessine lui-même fond, bordure, angles et marge intérieure de la vue ; sinon une enveloppe s'en charge.</summary>
    public virtual bool DrawsDecoration => false;

    /// <summary>Souplesse que le contrôle tient de ses enfants : une pile qui contient un <c>Spacer()</c> s'étire, comme dans SwiftUI.</summary>
    public virtual (bool Width, bool Height) FlexOf(Node node, View view) => (false, false);

    /// <summary>Part de place que le contrôle tient de ses enfants quand la vue n'en déclare pas (un ZStack, celle de son contenu).</summary>
    public virtual float GrowOf(Node node) => 0;

    /// <summary>Le nœud qui occupait la place peut-il afficher cette vue (même sorte de contrôle) ?</summary>
    public virtual bool CanReuse(Node existing, View view) => true;

    public static ViewAdapter For(View view) => view switch
    {
        TextView => TextAdapter.Instance,
        ButtonView => ButtonAdapter.Instance,
        StackView => StackAdapter.Instance,
        SpacerView => SpacerAdapter.Instance,
        TextFieldView => TextFieldAdapter.Instance,
        CheckboxView => CheckboxAdapter.Instance,
        DividerView => DividerAdapter.Instance,
        BoxView => BoxAdapter.Instance,
        ZStackView => ZStackAdapter.Instance,
        CanvasView => CanvasAdapter.Instance,
        MenuBarView => MenuBarAdapter.Instance,
        EditorView => EditorAdapter.Instance,
        NativeView => NativeAdapter.Instance,
        _ => throw new NotSupportedException(
            $"The view '{view.GetType().Name}' is not supported. Wrap an Avalonia control with 'Native' to display it."),
    };
}

/// <summary>Adaptateur typé : la vue et le contrôle arrivent sous leur vrai type.</summary>
internal abstract class ViewAdapter<TView, TControl> : ViewAdapter
    where TView : View
    where TControl : Control, new()
{
    public sealed override Control Create(Reconciler reconciler, Node node)
    {
        var control = new TControl();
        Attach(reconciler, node, control);
        return control;
    }

    public sealed override void Update(Reconciler reconciler, Node node, View view, ViewEnvironment env) =>
        Update(reconciler, node, (TControl)node.Content, (TView)view, env);

    public sealed override (bool Width, bool Height) FlexOf(Node node, View view) => FlexOf(node, (TView)view);

    /// <summary>Branche une fois pour toutes les événements du contrôle ; ils lisent la vue courante du nœud.</summary>
    protected virtual void Attach(Reconciler reconciler, Node node, TControl control) { }

    protected abstract void Update(Reconciler reconciler, Node node, TControl control, TView view, ViewEnvironment env);

    protected virtual (bool Width, bool Height) FlexOf(Node node, TView view) => (false, false);
}
