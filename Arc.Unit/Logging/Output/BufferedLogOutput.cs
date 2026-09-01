// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Base class of <see cref="ILogOutput"/> which buffers logs and writes them later.<br/>
/// Buffered outputs are flushed by <see cref="LogUnit.Flush()"/> and <see cref="LogUnit.FlushAndTerminate()"/>.
/// </summary>
public abstract class BufferedLogOutput : ILogOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BufferedLogOutput"/> class,<br/>
    /// and registers it to <see cref="LogUnit"/> as a flush target.
    /// </summary>
    /// <param name="logUnit"><see cref="LogUnit"/>.</param>
    public BufferedLogOutput(LogUnit logUnit)
    {
        logUnit.RegisterFlushTarget(this);
    }

    /// <summary>
    /// Writes the buffered logs to the log output.
    /// </summary>
    /// <param name="terminate"><see langword="true" /> to write all the buffered logs and terminate the log worker.</param>
    /// <returns>The number of flushed logs.</returns>
    public abstract Task<int> Flush(bool terminate);

    /// <inheritdoc/>
    public virtual void Output(LogEvent logEvent)
    {
        throw new NotImplementedException();
    }
}
