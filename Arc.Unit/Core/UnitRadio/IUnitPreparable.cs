// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrossChannel;

namespace Arc.Unit;

/// <summary>
/// An interface for the preparation process of unit objects.
/// </summary>
[RadioServiceInterface]
public interface IUnitPreparable : IRadioService
{
    /// <summary>
    /// Performs the initialization process for unit objects.<br/>
    /// This method is called once at the very beginning.
    /// </summary>
    /// <param name="unitContext">the <see cref="UnitContext"/> associated with this operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous load operation.</returns>
    public Task Prepare(UnitContext unitContext, CancellationToken cancellationToken);
}
