using Mvu.Primitives;
using System.Collections.Concurrent;
using System.Reflection;

namespace Mvu.Rendering;

/// <summary>
/// Polices fournies par l'application (fichiers .ttf / .otf), prioritaires sur celles du système.
/// <code>
/// Fonts.Register("Inter", "Assets/Inter-Regular.ttf");
/// Fonts.Register("Inter", "Assets/Inter-SemiBold.ttf", weight: 600);
/// var theme = Theme.Light.WithFont("Inter");
/// </code>
/// Pour une graisse non fournie, la plus proche enregistrée est utilisée.
/// </summary>
public static class Fonts
{
    static readonly ConcurrentDictionary<(string Family, int Weight), byte[]> Faces = new();

    /// <summary>Change à chaque enregistrement : les moteurs de rendu vident alors leur cache de polices.</summary>
    public static int Version { get; private set; }

    public static void Register(string family, string path, int weight = FontWeight.Regular) =>
        Register(family, File.ReadAllBytes(path), weight);

    public static void Register(string family, Stream stream, int weight = FontWeight.Regular)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        Register(family, copy.ToArray(), weight);
    }

    /// <summary>Police embarquée comme ressource de l'assembly (<c>&lt;EmbeddedResource Include="Assets/Inter.ttf" /&gt;</c>).</summary>
    public static void RegisterResource(string family, Assembly assembly, string resourceName, int weight = FontWeight.Regular)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new ArgumentException($"Resource '{resourceName}' was not found in assembly '{assembly.GetName().Name}'.", nameof(resourceName));
        Register(family, stream, weight);
    }

    public static void Register(string family, byte[] data, int weight = FontWeight.Regular)
    {
        Faces[(family.ToLowerInvariant(), weight)] = data;
        Version++;
    }

    /// <summary>Données de la police enregistrée pour cette famille, à la graisse la plus proche.</summary>
    public static byte[]? Find(string family, int weight)
    {
        string key = family.ToLowerInvariant();
        byte[]? best = null;
        int bestDistance = int.MaxValue;
        foreach (var ((f, w), data) in Faces)
        {
            if (f != key) continue;
            int d = Math.Abs(w - weight);
            if (d < bestDistance) { bestDistance = d; best = data; }
        }
        return best;
    }
}
