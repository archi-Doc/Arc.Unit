// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Represents a single log entry passed from a <see cref="LogWriter"/> to an <see cref="ILogOutput"/>.
/// </summary>
public readonly struct LogEvent : IEquatable<LogEvent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogEvent"/> struct.<br/>
    /// <see cref="Timestamp"/> is set to the current time.
    /// </summary>
    /// <param name="logService">The log service which created this event.</param>
    /// <param name="logSourceType">The log source type (the category of the log).</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event id (0 if not specified).</param>
    /// <param name="message">The log message.</param>
    public LogEvent(ILogService logService, Type logSourceType, LogLevel logLevel, long eventId, string message)
    {
        this.LogService = logService;
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
        this.EventId = eventId;
        this.Message = message;
        this.Timestamp = DateTimeOffset.UtcNow.AddTicks(LogUnit.OffsetTicks);
    }

    /// <summary>
    /// The log service which created this event (provides access to <see cref="IConsoleService"/> and other loggers).
    /// </summary>
    public readonly ILogService LogService;

    /// <summary>
    /// The log source type (the category of the log).
    /// </summary>
    public readonly Type LogSourceType;

    /// <summary>
    /// The log level.
    /// </summary>
    public readonly LogLevel LogLevel;

    /// <summary>
    /// The event id (0 if not specified).
    /// </summary>
    public readonly long EventId;

    /// <summary>
    /// The log message.
    /// </summary>
    public readonly string Message;

    /// <summary>
    /// The UTC time when this event was created (<see cref="LogUnit.SetTimeOffset(TimeSpan)"/> is applied).
    /// </summary>
    public readonly DateTimeOffset Timestamp;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is LogEvent other && this.Equals(other);

    /// <summary>
    /// Determines whether the specified event has the same source, level, event id and message (<see cref="Timestamp"/> is not compared).
    /// </summary>
    /// <param name="other">The event to compare with.</param>
    /// <returns><see langword="true"/> if the events are equivalent.</returns>
    public bool Equals(LogEvent other)
        => this.LogSourceType == other.LogSourceType &&
        this.LogLevel == other.LogLevel &&
        this.EventId == other.EventId &&
        this.Message == other.Message;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel, this.EventId, this.Message);
}
