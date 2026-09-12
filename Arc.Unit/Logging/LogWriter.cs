// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Provides a lightweight writer that emits log messages through an associated <see cref="LogBroker"/>.
/// </summary>
/// <remarks>
/// This type is immutable and delegates logging behavior to the broker configuration,
/// including optional filtering and output routing. A default writer discards messages.
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
    /// instance that controls the final log level and destination. The original source and service are preserved;
    /// the destination filter is not applied again.
    /// </remarks>
    public void Write(string message, long eventId = default)
    {
        var broker = this.logBroker;
        if (broker is null)
        {
            return;
        }

        if (broker.FilterDelegate is not null)
        {// Filter -> Log
            if (broker.FilterDelegate(new(this.logService, broker.LogSourceType, broker.LogLevel, eventId, this)) is LogWriter loggerInstance &&
                loggerInstance.logBroker is { } filteredBroker)
            {// A default LogWriter (no broker) is treated as 'no log'.
                filteredBroker.LogDelegate(new(this.logService, broker.LogSourceType, filteredBroker.LogLevel, eventId, message));
            }
        }
        else
        {// Log
            broker.LogDelegate(new(this.logService, broker.LogSourceType, broker.LogLevel, eventId, message));
        }
    }

    private readonly ILogService logService;
    private readonly LogBroker logBroker;

    /// <summary>
    /// Gets the output type, or <see cref="EmptyLogOutput"/> for a default writer.
    /// </summary>
    public Type OutputType => this.logBroker?.OutputType ?? typeof(EmptyLogOutput);
}
