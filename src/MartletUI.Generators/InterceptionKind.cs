// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Generators;

/// <summary>
/// Specifies what the interceptor of a call in a view builder block does.
/// </summary>
internal enum InterceptionKind
{
    /// <summary>
    /// Runs the call and adds its view to the block: <c>Text("a");</c>, or the last modifier of
    /// <c>Progress().Padding(8);</c>.
    /// </summary>
    Emit,

    /// <summary>
    /// Adds to the block a scope that runs the view method later: <c>Progress();</c>.
    /// </summary>
    EmitScope,

    /// <summary>
    /// Returns a scope that runs the view method later, without adding it: <c>Progress()</c> in
    /// <c>Progress().Padding(8);</c>, whose modifiers apply to the scope.
    /// </summary>
    Scope,
}
