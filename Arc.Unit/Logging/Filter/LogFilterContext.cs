// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Represents the information passed to <see cref="ILogFilter.Filter(LogFilterParameter)"/>.
/// </summary>
public readonly struct LogFilterParameter : IEquatable<LogFilterParameter>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogFilterParameter"/> struct.
    /// </summary>
    /// <param name="logService">The log service which created this parameter.</param>
    /// <param name="logSourceType">The log source type (the category of the log).</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event id (0 if not specified).</param>
    /// <param name="originalWriter">The writer which is going to be used if the filter does not change it.</param>
    public LogFilterParameter(ILogService logService, Type logSourceType, LogLevel logLevel, long eventId, LogWriter originalWriter)
    {
        this.LogService = logService;
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
        this.EventId = eventId;
        this.OriginalWriter = originalWriter;
    }

    /// <summary>
    /// The log service which created this parameter (use it to obtain another <see cref="LogWriter"/>).
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
        => obj is LogFilterParameter other && this.Equals(other);

    /// <summary>
    /// Determines whether the specified parameter has the same source, level, event id and writer.
    /// </summary>
    /// <param name="other">The parameter to compare with.</param>
    /// <returns><see langword="true"/> if the parameters are equivalent.</returns>
    public bool Equals(LogFilterParameter other)
        => this.LogSourceType == other.LogSourceType &&
        this.LogLevel == other.LogLevel &&
        this.EventId == other.EventId &&
        this.OriginalWriter == other.OriginalWriter;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel, this.EventId, this.OriginalWriter);
}
