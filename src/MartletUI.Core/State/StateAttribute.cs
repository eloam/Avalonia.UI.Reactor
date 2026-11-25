// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace MartletUI.State;

/// <summary>
/// Declares an observed state property: <c>[State] partial int Count { get; set; }</c>.
/// </summary>
/// <remarks>
/// The source generator writes the body of the property: reading the state from a <c>Body</c> subscribes the component,
/// and changing it, from anywhere and on any thread, re-evaluates only the components that read it. The class must be
/// <see langword="partial"/>. A settable property also gets a <see cref="Binding{T}"/>, under <c>Bind</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class StateAttribute : Attribute;
