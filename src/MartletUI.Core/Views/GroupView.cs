// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.Views;

/// <summary>
/// Represents several views presented as one.
/// </summary>
/// <remarks>
/// In a stack, the children of a group take their places directly among the children of the stack:
/// <c>VStack(Header(), ForEach(todos, Row), Footer())</c>. Outside a stack, or with modifiers, a group stacks its
/// children vertically.
/// </remarks>
/// <param name="Children">The views of the group.</param>
public sealed record GroupView(IReadOnlyList<View> Children) : View;
