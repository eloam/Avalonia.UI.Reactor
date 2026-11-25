// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.State;

/// <summary>
/// Represents the outcome of the last update, for tests and diagnostics.
/// </summary>
/// <param name="Evaluated">The number of scopes whose body was re-evaluated.</param>
/// <param name="Reused">The number of scopes kept as they were.</param>
/// <param name="Scopes">The total number of scopes displayed.</param>
internal readonly record struct UpdateStats(int Evaluated, int Reused, int Scopes);
