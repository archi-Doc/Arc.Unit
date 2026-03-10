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
    /// <param name="message">The message.</param>
    /// <param name="eventId">The event id.</param>
    public void Log(string message, long eventId = default);

    public Type OutputType { get; }
}
