// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

namespace TodoDemo.Models;

/// <summary>
/// Specifies which tasks the list shows.
/// </summary>
public enum Filter
{
    /// <summary>
    /// Every task.
    /// </summary>
    All,

    /// <summary>
    /// The tasks still to do.
    /// </summary>
    Active,

    /// <summary>
    /// The completed tasks.
    /// </summary>
    Done,
}
