// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Represents the lifecycle state of a unit.
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
    Rip,
}
