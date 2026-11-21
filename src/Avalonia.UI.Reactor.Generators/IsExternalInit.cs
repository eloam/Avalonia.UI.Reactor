// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace System.Runtime.CompilerServices;

/// <summary>
/// Enables <see langword="record"/> types and <see langword="init"/> accessors in this project.
/// </summary>
/// <remarks>
/// The compiler marks <see langword="init"/> accessors with this type, which .NET 5 and later provide but netstandard2.0
/// does not. Source generators must target netstandard2.0 because they run inside the compiler, on .NET Framework as
/// well as on .NET, so the type is declared here, internal to the generator.
/// </remarks>
internal static class IsExternalInit;
