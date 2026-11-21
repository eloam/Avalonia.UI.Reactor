// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace Avalonia.UI.Reactor.State;

/// <summary>
/// Represents a unit of re-evaluation: a component, a <c>ForEach</c> item, an <c>Observe</c> block, the drawing of a
/// <c>Canvas</c> or the update of a <c>Native</c> view.
/// </summary>
/// <remarks>
/// A scope records the signals read during its last evaluation. Only a change to one of them marks it dirty, which then
/// calls <c>requestUpdate</c> from the thread that changed the state.
/// </remarks>
/// <param name="requestUpdate">The action that schedules an update of the host.</param>
/// <param name="id">The path of the scope in the tree of views.</param>
/// <param name="identity">The object that identifies the scope (a component), or <see langword="null"/>.</param>
internal sealed class Scope(Action requestUpdate, string id, object? identity)
{
    private readonly HashSet<Signal> _dependencies = [];
    private volatile bool _dirty = true;

    /// <summary>
    /// Gets the scope being evaluated on this thread, or <see langword="null"/>.
    /// </summary>
    [field: ThreadStatic]
    internal static Scope? Current { get; private set; }

    /// <summary>
    /// Gets the path of the scope in the tree of views.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// Gets the object that identifies the scope (a component), or <see langword="null"/>.
    /// </summary>
    public object? Identity { get; } = identity;

    /// <summary>
    /// Gets or sets the view produced by the last evaluation.
    /// </summary>
    public View? Content { get; set; }

    /// <summary>
    /// Gets or sets the scope view displayed by the last update.
    /// </summary>
    public ScopeView? LastView { get; set; }

    /// <summary>
    /// Gets or sets the environment of the last evaluation, used by the reconciler to reuse the scope as it is.
    /// </summary>
    public ViewEnvironment? Environment { get; set; }

    /// <summary>
    /// Gets or sets the scopes nested in this one at the last evaluation.
    /// </summary>
    public List<Scope> Descendants { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of the last update that visited this scope.
    /// </summary>
    public int LastVisit { get; set; }

    /// <summary>
    /// Gets a value indicating whether some state read by this scope changed since its last evaluation.
    /// </summary>
    public bool IsDirty => _dirty;

    /// <summary>
    /// Marks this scope for re-evaluation and requests an update. This method can be called from any thread.
    /// </summary>
    public void MarkDirty()
    {
        _dirty = true;
        requestUpdate();
    }

    /// <summary>
    /// Evaluates a function while recording the signals it reads.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="build">The function to evaluate, typically a body.</param>
    /// <returns>The result of <paramref name="build"/>.</returns>
    public T Evaluate<T>(Func<T> build)
    {
        // Cleared before the evaluation: a concurrent change marks the scope dirty again.
        _dirty = false;
        ClearDependencies();
        Scope? previous = Current;
        Current = this;
        try
        {
            return build();
        }
        finally
        {
            Current = previous;
        }
    }

    /// <summary>
    /// Runs some work, such as drawing, while recording the signals it reads.
    /// </summary>
    /// <param name="work">The work to run.</param>
    public void Evaluate(Action work) => Evaluate(() =>
    {
        work();
        return true;
    });

    /// <summary>
    /// Unsubscribes this scope from every signal it read, so that a removed scope is no longer kept alive by its state.
    /// </summary>
    public void Release() => ClearDependencies();

    /// <summary>
    /// Records that this scope depends on a signal.
    /// </summary>
    /// <param name="signal">The signal read during the evaluation.</param>
    internal void Track(Signal signal)
    {
        lock (_dependencies)
        {
            _dependencies.Add(signal);
        }
    }

    private void ClearDependencies()
    {
        Signal[] dependencies;
        lock (_dependencies)
        {
            dependencies = [.. _dependencies];
            _dependencies.Clear();
        }

        foreach (Signal signal in dependencies)
        {
            signal.Unsubscribe(this);
        }
    }
}
