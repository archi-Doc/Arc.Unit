// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Represents the information passed to <see cref="ILogFilter.Filter(LogFilterContext)"/>.
/// </summary>
public readonly struct LogFilterContext : IEquatable<LogFilterContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogFilterContext"/> struct.
    /// </summary>
    /// <param name="logService">The log service which created this context.</param>
    /// <param name="logSourceType">The log source type (the category of the log).</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event id (0 if not specified).</param>
    /// <param name="originalWriter">The writer which is going to be used if the filter does not change it.</param>
    public LogFilterContext(ILogService logService, Type logSourceType, LogLevel logLevel, long eventId, LogWriter originalWriter)
    {
        this.LogService = logService;
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
        this.EventId = eventId;
        this.OriginalWriter = originalWriter;
    }

    /// <summary>
    /// The log service which created this context (use it to obtain another <see cref="LogWriter"/>).
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
    /// The writer which is going to be used if the filter does not change it.
    /// </summary>
    public readonly LogWriter OriginalWriter;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is LogFilterContext other && this.Equals(other);

    /// <summary>
    /// Determines whether the specified context has the same source, level, event id and writer.
    /// </summary>
    /// <param name="other">The context to compare with.</param>
    /// <returns><see langword="true"/> if the contexts are equivalent.</returns>
    public bool Equals(LogFilterContext other)
        => this.LogSourceType == other.LogSourceType &&
        this.LogLevel == other.LogLevel &&
        this.EventId == other.EventId &&
        this.OriginalWriter == other.OriginalWriter;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel, this.EventId, this.OriginalWriter);
}
