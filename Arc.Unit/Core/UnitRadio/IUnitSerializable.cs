// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrossChannel;

namespace Arc.Unit;

/// <summary>
/// An interface for the serialization process of unit objects.
/// </summary>
[RadioServiceInterface]
public interface IUnitSerializable : IRadioService
{
    /// <summary>
    /// Performs the loading process for unit objects.<br/>
    /// This method is called once, following <see cref="IUnitPreparable.Prepare(UnitMessage.Prepare)"/>.<br/>
    /// Throw <see cref="Arc.Threading.PanicException"/> to abort the procedure.
    /// </summary>
    /// <param name="message">Unit message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task LoadAsync(UnitMessage.LoadAsync message, CancellationToken cancellationToken);

    /// <summary>
    ///  Performs the saving process for unit objects.<br/>
    ///  This method is called after <see cref="IUnitExecutable.StartAsync(UnitMessage.StartAsync, CancellationToken)"/> and may be called once or multiple times.<br/>
    /// </summary>
    /// <param name="message">Unit message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SaveAsync(UnitMessage.SaveAsync message, CancellationToken cancellationToken);
}
