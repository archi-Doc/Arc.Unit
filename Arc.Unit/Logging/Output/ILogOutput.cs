// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Interface for receiving and outputting log events.<br/>
/// An implementation is registered in the DI container, and selected by <see cref="LoggerResolverDelegate"/>.
/// </summary>
public interface ILogOutput
{
    internal delegate void OutputDelegate(LogEvent logEvent);

    /// <summary>
    /// Writes the log event to the destination of this output.<br/>
    /// This method may be called from multiple threads simultaneously.
    /// </summary>
    /// <param name="logEvent">The log event to be written.</param>
    public void Output(LogEvent logEvent);
}
