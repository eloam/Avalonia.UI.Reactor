namespace System.Runtime.CompilerServices;

/// <summary>
/// Absent de netstandard2.0 (cible imposée aux générateurs, qui s'exécutent dans le compilateur) :
/// nécessaire pour y écrire des <c>record</c> et des propriétés <c>init</c>.
/// </summary>
internal static class IsExternalInit;
