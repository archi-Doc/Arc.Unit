// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrossChannel;

namespace Arc.Unit;

/// <summary>
/// An interface for the execution process of unit objects.
/// </summary>
[RadioService]
public interface IUnitExecutable : IRadioService
{
    /// <summary>
    /// Performs the start-up process for the unit objects.<br/>
    /// This method is called after <see cref="IUnitSerializable.Load(UnitContext, CancellationToken)"/> and may be called once or multiple times.<br/>
    /// </summary>
    /// <param name="unitContext">the <see cref="UnitContext"/> associated with this operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous load operation.</returns>
    public Task Start(UnitContext unitContext, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the suspension process for unit objects.<br/>
    /// This method is called after <see cref="IUnitExecutable.Start(UnitContext, CancellationToken)"/>.
    /// </summary>
    /// <param name="unitContext">the <see cref="UnitContext"/> associated with this operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous load operation.</returns>
    public Task Stop(UnitContext unitContext, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the termination process for unit objects.<br/>
    /// Called only once at the beginning of the termination process.
    /// </summary>
    /// <param name="unitContext">the <see cref="UnitContext"/> associated with this operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous load operation.</returns>
    public Task Terminate(UnitContext unitContext, CancellationToken cancellationToken);
}
