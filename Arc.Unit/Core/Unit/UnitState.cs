// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Optional application-managed lifecycle state. Arc.Unit does not update this value automatically.
/// </summary>
public enum UnitState : byte
{
    /// <summary>
    /// The object is in its initial state and has not yet been activated.
    /// </summary>
    Initial,

    /// <summary>
    /// The object is active and valid.
    /// </summary>
    Active,

    /// <summary>
    /// The object has been disposed and cannot be reused.
    /// </summary>
    Disposed,
}
