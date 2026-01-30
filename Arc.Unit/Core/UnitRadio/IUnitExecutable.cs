// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrossChannel;

namespace Arc.Unit;

/// <summary>
/// An interface for the execution process of unit objects.
/// </summary>
[RadioServiceInterface]
public interface IUnitExecutable : IRadioService
{
    /// <summary>
    /// Performs the start-up process for the unit objects.<br/>
    /// This method is called after <see cref="IUnitSerializable.LoadAsync(UnitMessage.LoadAsync, CancellationToken)"/> and may be called once or multiple times.<br/>
    /// </summary>
    /// <param name="message">Unit message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task StartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the suspension process for unit objects.<br/>
    /// This method is called after <see cref="IUnitExecutable.StartAsync(UnitMessage.StartAsync, CancellationToken)"/>.
    /// </summary>
    /// <param name="message">Unit message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Stop(UnitMessage.Stop message);

    /// <summary>
    /// Performs the termination process for unit objects.<br/>
    /// Called only once at the beginning of the termination process.
    /// </summary>
    /// <param name="message">Unit message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task TerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken);
}
