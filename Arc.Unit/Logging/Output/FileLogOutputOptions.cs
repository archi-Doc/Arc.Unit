// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Options of <see cref="FileLogger{TOption}"/>.
/// </summary>
public record class FileLoggerOptions
{
    /// <summary>
    /// The default log file path.
    /// </summary>
    public const string DefaultPath = "Log.txt";

    /// <summary>
    /// The default value of <see cref="MaxQueue"/>.
    /// </summary>
    public const int DefaultMaxQueue = 1_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggerOptions"/> class.
    /// </summary>
    public FileLoggerOptions()
    {
        this.FormatterOptions = new SimpleLogFormatterOptions(false) with
        {
            TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff K",
        };
    }

    /// <summary>
    /// Gets the log file path (the date is inserted before the extension: "Log.txt" -> "Log20260101.txt").<br/>
    /// A relative path is combined with the current directory.
    /// </summary>
    public string Path { get; init; } = DefaultPath;

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions FormatterOptions { get; init; }

    /// <summary>
    /// Gets the maximum queued event count (zero or negative means unlimited). New events are dropped when full.
    /// </summary>
    public int MaxQueue { get; init; } = DefaultMaxQueue;

    /// <summary>
    /// Gets the upper limit of log capacity in megabytes.<br/>
    /// One megabyte is 1,000,000 bytes. Periodic cleanup deletes oldest daily files until within the limit.
    /// Zero or negative values retain no files at cleanup; they do not disable cleanup.
    /// </summary>
    public int MaxLogCapacity { get; init; } = 10;

    /// <summary>
    /// Gets a value indicating whether or not to clear logs at startup.
    /// </summary>
    public bool ClearLogsAtStartup { get; init; } = false;
}
