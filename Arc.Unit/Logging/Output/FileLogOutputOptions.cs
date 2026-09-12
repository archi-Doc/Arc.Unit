// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Options of <see cref="FileLogOutput{TOptions}"/>.
/// </summary>
public record class FileLogOutputOptions
{
    /// <summary>
    /// The default log file path.
    /// </summary>
    public const string DefaultFilePath = "Log.txt";

    /// <summary>
    /// The default value of <see cref="MaxQueueLength"/>.
    /// </summary>
    public const int DefaultMaxQueueLength = 1_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogOutputOptions"/> class.
    /// </summary>
    public FileLogOutputOptions()
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
    public string FilePath { get; init; } = DefaultFilePath;

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions FormatterOptions { get; init; }

    /// <summary>
    /// Gets the maximum queued event count (zero or negative means unlimited). New events are dropped when full.
    /// </summary>
    public int MaxQueueLength { get; init; } = DefaultMaxQueueLength;

    /// <summary>
    /// Gets the upper limit of log capacity in megabytes.<br/>
    /// One megabyte is 1,000,000 bytes. Periodic cleanup deletes oldest daily files until within the limit.
    /// Zero or negative values retain no files at cleanup; they do not disable cleanup.
    /// </summary>
    public int MaxLogCapacityInMegabytes { get; init; } = 10;

    /// <summary>
    /// Gets a value indicating whether or not to clear logs at startup.
    /// </summary>
    public bool ClearLogsAtStartup { get; init; } = false;
}
