// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using MartletUI.Views;

namespace MartletUI.Runtime;

/// <summary>
/// Provides what the code generated for view builder blocks calls: it collects the views that the statements of a block
/// emit.
/// </summary>
/// <remarks>
/// Applications do not call it: a container that receives a block (<c>VStack(() =&gt; { … })</c>) runs it, and the
/// interceptors written by the generator for its view statements emit their views here.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ViewCollector
{
    // The assemblies compiled with the generator: a block of another assembly would lose its views without an error.
    private static readonly ConcurrentDictionary<Assembly, byte> s_assemblies = new();

    // The views of the block being run on this thread; a block run inside another one collects its own views.
    [field: ThreadStatic]
    private static List<View>? Current { get; set; }

    /// <summary>
    /// Records that an assembly was compiled with the generator; called by the module initializer that it writes.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    public static void RegisterAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        s_assemblies.TryAdd(assembly, 0);
    }

    /// <summary>
    /// Adds a view to the block being run; called by the interceptor of a view statement.
    /// </summary>
    /// <typeparam name="T">The type of the view.</typeparam>
    /// <param name="view">The view created by the statement.</param>
    /// <returns><paramref name="view"/>, as the statement returns it.</returns>
    /// <exception cref="InvalidOperationException">The block runs outside a container.</exception>
    public static T Emit<T>(T view)
        where T : View
    {
        (Current ?? throw new InvalidOperationException(
            "A view builder block ran outside a container. Pass the block to a container such as 'VStack', which collects its views.")).Add(view);
        return view;
    }

    /// <summary>
    /// Creates the scope of a view method called as a statement of a block; called by its interceptor.
    /// </summary>
    /// <remarks>
    /// The method runs later, when the reconciler applies the scope: what it reads belongs to the scope, which is
    /// re-evaluated alone when that state changes, and skipped when its caller is re-evaluated with equal arguments.
    /// </remarks>
    /// <param name="method">The name of the method, which identifies the scope among the children of its container.</param>
    /// <param name="inputs">The receiver and the arguments of the call, as a tuple compared with
    /// <see cref="object.Equals(object?, object?)"/>.</param>
    /// <param name="build">The call of the method.</param>
    /// <returns>The scope.</returns>
    public static ScopeView ViewMethod(string method, object inputs, Func<View> build) =>
        new(build) { Identity = method, Inputs = inputs };

    /// <summary>
    /// Adds a view to the block being run, for <see cref="Ui.Show(View)"/>.
    /// </summary>
    /// <param name="view">The view, or <see langword="null"/> to add nothing.</param>
    /// <exception cref="InvalidOperationException">No block is being run.</exception>
    internal static void Show(View? view)
    {
        List<View> views = Current ?? throw new InvalidOperationException(
            "'Show' was called outside a view builder block. Call it in a block passed to a container such as 'VStack', or pass the view to the container.");
        if (view is not null)
            views.Add(view);
    }

    /// <summary>
    /// Runs a block and returns the views that its statements emitted.
    /// </summary>
    /// <param name="block">The block, whose assembly must have been compiled with the generator.</param>
    /// <param name="run">The function that runs the block (with its argument, for the row of a list).</param>
    /// <returns>The views of the block, in order.</returns>
    /// <exception cref="InvalidOperationException">The assembly of the block was compiled without the generator.</exception>
    internal static View[] Collect(Delegate block, Action run)
    {
        Assembly assembly = block.Method.Module.Assembly;
        if (!s_assemblies.ContainsKey(assembly))
        {
            throw new InvalidOperationException(
                $"The view builder block of '{assembly.GetName().Name}' was compiled without the generator of " +
                "MartletUI, so its views would be lost. Reference the 'MartletUI.Core' package, which runs " +
                "the generator, and keep source generators enabled.");
        }

        List<View>? previous = Current;
        var views = new List<View>();
        Current = views;
        try
        {
            run();
        }
        finally
        {
            Current = previous;
        }

        return [.. views];
    }
}
