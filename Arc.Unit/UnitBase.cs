// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Base class of Unit.<br/>
/// Unit is an independent unit of function and dependency.<br/>
/// By implementing <see cref="IUnitPreparable"/> and other interfaces, methods can be called from <see cref="UnitContext"/>.
/// </summary>
public abstract class UnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBase"/> class.
    /// </summary>
    /// <param name="context"><see cref="UnitContext"/>.</param>
    public UnitBase(UnitContext context)
    {
        context.AddRadio(this);
    }
}

/// <summary>
/// An interface for the preparation process of unit objects.
/// </summary>
public interface IUnitPreparable
{
    /// <summary>
    /// Performs the initialization process for unit objects.<br/>
    /// This method is called once at the very beginning.
    /// </summary>
    /// <param name="message">Unit message.</param>
    public void Prepare(UnitMessage.Prepare message);
}

/// <summary>
/// An interface for the execution process of unit objects.
/// </summary>
public interface IUnitExecutable
{
    /// <summary>
    /// Performs the start-up process for the unit objects.<br/>
    /// This method is called after <see cref="IUnitSerializable.LoadAsync(UnitMessage.LoadAsync, CancellationToken)"/> and may be called once or multiple times.<br/>
    /// </summary>
    /// <param name="message">Unit message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    public Task StartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the suspension process for unit objects.<br/>
    /// This method is called after <see cref="IUnitExecutable.StartAsync(UnitMessage.StartAsync, CancellationToken)"/>.
    /// </summary>
    /// <param name="message">Unit message.</param>
    public void Stop(UnitMessage.Stop message);

    /// <summary>
    /// Performs the termination process for unit objects.<br/>
    /// Called only once at the beginning of the termination process.
    /// </summary>
    /// <param name="message">Unit message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    public Task TerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken);
}

/// <summary>
/// An interface for the serialization process of unit objects.
/// </summary>
public interface IUnitSerializable
{
    /// <summary>
    /// Performs the loading process for unit objects.<br/>
    /// This method is called once, following <see cref="IUnitPreparable.Prepare(UnitMessage.Prepare)"/>.<br/>
    /// Throw <see cref="Arc.Threading.PanicException"/> to abort the procedure.
    /// </summary>
    /// <param name="message">Unit message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    public Task LoadAsync(UnitMessage.LoadAsync message, CancellationToken cancellationToken);

    /// <summary>
    ///  Performs the saving process for unit objects.<br/>
    ///  This method is called after <see cref="IUnitExecutable.StartAsync(UnitMessage.StartAsync, CancellationToken)"/> and may be called once or multiple times.<br/>
    /// </summary>
    /// <param name="message">Unit message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    public Task SaveAsync(UnitMessage.SaveAsync message, CancellationToken cancellationToken);
}
