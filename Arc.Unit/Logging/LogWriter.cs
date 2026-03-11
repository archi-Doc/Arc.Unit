// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Provides a lightweight writer that emits log messages through an associated <see cref="LogBroker"/>.
/// </summary>
/// <remarks>
/// This type is immutable and delegates logging behavior to the broker configuration,
/// including optional filtering and output routing.
/// </remarks>
public readonly record struct LogWriter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogWriter"/> struct.
    /// </summary>
    /// <param name="logService">The logging service that owns this writer context.</param>
    /// <param name="logBroker">The broker that defines source type, level, filtering, and output behavior.</param>
    internal LogWriter(ILogService logService, LogBroker logBroker)
    {
        this.logService = logService;
        this.logBroker = logBroker;
    }

    /// <summary>
    /// Writes a log message using the current broker configuration.
    /// </summary>
    /// <param name="message">The message text to write.</param>
    /// <param name="eventId">An optional event identifier used to correlate log entries.</param>
    /// <remarks>
    /// When a filter delegate is configured, the filter may return a different <see cref="LogWriter"/>
    /// instance that controls the final log level and delegate used for output.
    /// </remarks>
    public void Log(string message, long eventId = default)
    {
        var broker = this.logBroker;
        LogEvent param = new(this.logService, broker.LogSourceType, broker.LogLevel, eventId, message);
        if (broker.FilterDelegate is not null)
        {// Filter -> Log
            if (broker.FilterDelegate(new(this.logService, broker.LogSourceType, broker.LogLevel, eventId, this)) is LogWriter loggerInstance)
            {
                loggerInstance.logBroker.LogDelegate(new(this.logService, broker.LogSourceType, loggerInstance.logBroker.LogLevel, eventId, message));
            }
        }
        else
        {// Log
            broker.LogDelegate(param);
        }
    }

    private readonly ILogService logService;
    private readonly LogBroker logBroker;

    /// <summary>
    /// Gets the output target type used by the current log broker.
    /// </summary>
    public Type OutputType => this.logBroker.OutputType;
}
