using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace Mvu.Desktop;

/// <summary>Outils COM communs aux interfaces générées par CsWin32 (Direct3D 12, DXGI).</summary>
internal static unsafe class Com
{
    /// <summary>IID de l'interface <typeparamref name="T"/>, à une adresse permanente (garantie par CsWin32).</summary>
    public static Guid* IidOf<T>() where T : unmanaged, IComIID =>
        (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in T.Guid));

    /// <summary>Libère la référence détenue et remet le pointeur à null (sans effet s'il l'est déjà).</summary>
    public static void Release<T>(ref T* pointer) where T : unmanaged
    {
        if (pointer is null) return;
        ((IUnknown*)pointer)->Release();
        pointer = null;
    }
}
