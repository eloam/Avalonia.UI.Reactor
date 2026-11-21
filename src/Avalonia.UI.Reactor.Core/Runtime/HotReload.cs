// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Reflection.Metadata;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.State;

[assembly: MetadataUpdateHandler(typeof(HotReload))]

namespace Avalonia.UI.Reactor.Runtime;

/// <summary>
/// Connects the framework to .NET Hot Reload (<c>dotnet watch</c>, Visual Studio, Rider).
/// </summary>
/// <remarks>
/// After each code change applied at run time, the runtime calls <see cref="UpdateApplication"/>: every open window
/// re-evaluates all its components. State is preserved; only the views are recomputed.
/// </remarks>
internal static class HotReload
{
    private static readonly List<WeakReference<IScopeHost>> s_hosts = [];

    /// <summary>
    /// Registers a host to re-evaluate after each code change. The host is held weakly.
    /// </summary>
    /// <param name="host">The host of an open window.</param>
    internal static void Register(IScopeHost host)
    {
        lock (s_hosts)
        {
            s_hosts.Add(new(host));
        }
    }

    /// <summary>
    /// Stops re-evaluating a host, typically when its window closes.
    /// </summary>
    /// <param name="host">The host to remove.</param>
    internal static void Unregister(IScopeHost host)
    {
        lock (s_hosts)
        {
            s_hosts.RemoveAll(w => !w.TryGetTarget(out IScopeHost? h) || ReferenceEquals(h, host));
        }
    }

    /// <summary>
    /// Called by the runtime, on any thread, before <see cref="UpdateApplication"/>. The framework has no cache to clear.
    /// </summary>
    /// <param name="_">The updated types, unused.</param>
    internal static void ClearCache(Type[]? _)
    {
    }

    /// <summary>
    /// Called by the runtime, on any thread, after a code change: asks every registered host to reload its views.
    /// </summary>
    /// <param name="_">The updated types, unused: every component is re-evaluated.</param>
    internal static void UpdateApplication(Type[]? _)
    {
        lock (s_hosts)
        {
            s_hosts.RemoveAll(w => !w.TryGetTarget(out IScopeHost? _));
            foreach (WeakReference<IScopeHost> reference in s_hosts)
            {
                if (reference.TryGetTarget(out IScopeHost? host))
                {
                    host.Reload();
                }
            }
        }
    }
}
