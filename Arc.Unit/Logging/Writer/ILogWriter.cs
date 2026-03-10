// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Interface for log writing.<br/>
/// Log levels and log output are fixed.
/// </summary>
public interface ILogWriter
{
    /// <summary>
    /// Send a log to the log output.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="message">The message.</param>
    public void Log(long eventId, string message);

    public Type OutputType { get; }
}
