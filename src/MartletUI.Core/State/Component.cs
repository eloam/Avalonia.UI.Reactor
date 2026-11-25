// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using MartletUI.Theming;
using MartletUI.Views;

namespace MartletUI.State;

/// <summary>
/// Represents a screen or a part of a screen: some state (<see cref="StateAttribute">[State]</see>) and a
/// <see cref="Body"/>.
/// </summary>
/// <remarks>
/// A component is placed in the view of another component like any view (<c>VStack(counter, …)</c>) and is re-evaluated
/// only when some state it read changes. It keeps its state as long as the same instance is displayed.
/// </remarks>
/// <example>
/// <code>
/// public sealed partial class Counter : Component
/// {
///     [State] partial int Count { get; set; }
///
///     public override View Body => Button($"{Count}", () => Count++);
/// }
/// </code>
/// </example>
public abstract class Component
{
    private readonly Signal _refresh = new("Refresh") { Change = "'Refresh' was called" };
    private ScopeView? _view;

    /// <summary>
    /// Gets the view of the component, computed from its state.
    /// </summary>
    /// <remarks>
    /// The body must be pure: it reads state but never changes it, and performs no input or output. A change of the state
    /// that a view depends on throws an <see cref="InvalidOperationException"/> while the body is computed.
    /// </remarks>
    public abstract View Body { get; }

    /// <summary>
    /// Gets or sets the host that displays this component; set when the component is displayed for the first time.
    /// </summary>
    internal IScopeHost? Host { get; set; }

    /// <summary>
    /// Gets the theme catalog of the application, for example to build a theme picker with
    /// <see cref="Themes.Families"/>.
    /// </summary>
    /// <remarks>
    /// To dress a view, the view declares its theme: <c>.Theme("Sable")</c>, <c>.ColorScheme(ColorScheme.Dark)</c>. The
    /// catalog is available once the component is displayed, in <see cref="Body"/> and in actions.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The component has not been displayed yet.</exception>
    protected Themes Themes => Host?.Themes
        ?? throw new InvalidOperationException("'Themes' is available only after the component has been displayed.");

    /// <summary>
    /// Converts a component into a view, so that it can be placed in another view; <see langword="null"/> stays
    /// <see langword="null"/> (<c>ShowCounter ? counter : null</c>).
    /// </summary>
    /// <param name="component">The component to place.</param>
    [return: NotNullIfNotNull(nameof(component))]
    public static implicit operator View?(Component? component) =>
        component is null ? null : component._view ??= new ScopeView(component.Render) { Identity = component };

    /// <summary>
    /// Re-evaluates this component. This method can be called from any thread.
    /// </summary>
    /// <remarks>
    /// <see cref="StateAttribute">[State]</see> properties and <see cref="StateList{T}"/> make this call unnecessary;
    /// it is meant for state that is not observed, such as a plain field or an external object.
    /// </remarks>
    protected void Refresh() => _refresh.Notify();

    /// <summary>
    /// Subscribes the scope being evaluated to <see cref="Refresh"/>.
    /// </summary>
    /// <remarks>
    /// The scope of a view method is skipped when the component is re-evaluated with the same arguments: it follows
    /// <see cref="Refresh"/> as well, so that a plain field that it reads is displayed again.
    /// </remarks>
    internal void TrackRefresh() => _refresh.Read();

    private View Render()
    {
        _refresh.Read();
        return Body;
    }
}
