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
    /// This method is called once, following <see cref="IUnitPreparable.Prepare(UnitContext, CancellationToken)"/>.<br/>
    /// Throw <see cref="Arc.Threading.PanicException"/> to abort the procedure.
    /// </summary>
    /// <param name="unitContext">the <see cref="UnitContext"/> associated with this operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous load operation.</returns>
    public Task Load(UnitContext unitContext, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the saving process for unit objects.<br/>
    /// This method is called after <see cref="IUnitExecutable.Start(UnitContext, CancellationToken)"/> and may be called once or multiple times.<br/>
    /// </summary>
    /// <param name="unitContext">the <see cref="UnitContext"/> associated with this operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous load operation.</returns>
    public Task Save(UnitContext unitContext, CancellationToken cancellationToken);
}
