using System.Reflection.Metadata;
using Avalonia.UI.Reactor.Runtime;
using Avalonia.UI.Reactor.State;

[assembly: MetadataUpdateHandler(typeof(HotReload))]

namespace Avalonia.UI.Reactor.Runtime;

/// <summary>
/// Branchement sur le Hot Reload de .NET (<c>dotnet watch</c>, Visual Studio, Rider).
/// Après chaque modification de code appliquée à chaud, le runtime appelle
/// <see cref="UpdateApplication"/> : chaque fenêtre ouverte, quel que soit son moteur, réévalue tous ses composants.
/// L'état est conservé, seule la vue est recalculée.
/// </summary>
internal static class HotReload
{
    static readonly List<WeakReference<IScopeHost>> Hosts = [];

    internal static void Register(IScopeHost host)
    {
        lock (Hosts) Hosts.Add(new(host));
    }

    internal static void Unregister(IScopeHost host)
    {
        lock (Hosts) Hosts.RemoveAll(w => !w.TryGetTarget(out var h) || ReferenceEquals(h, host));
    }

    // Appelés par le runtime, sur un thread quelconque.
    internal static void ClearCache(Type[]? types) { }

    internal static void UpdateApplication(Type[]? types)
    {
        lock (Hosts)
        {
            Hosts.RemoveAll(w => !w.TryGetTarget(out _));
            foreach (var w in Hosts)
                if (w.TryGetTarget(out var host))
                    host.Reload();
        }
    }
}
