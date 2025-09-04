// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class FileLoggerOptions
{
    public const string DefaultPath = "Log.txt";
    public const int DefaultMaxQueue = 1_000;

    public FileLoggerOptions()
    {
        this.Formatter = new SimpleLogFormatterOptions(false) with
        {
            TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff K",
        };
    }

    public string Path { get; init; } = DefaultPath;

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions Formatter { get; init; }

    /// <summary>
    /// Gets the maximum number of queued log (0 for unlimited).
    /// </summary>
    public int MaxQueue { get; init; } = DefaultMaxQueue;

    /// <summary>
    /// Gets the upper limit of log capacity in megabytes.
    /// </summary>
    public int MaxLogCapacity { get; init; } = 10;

    /// <summary>
    /// Gets a value indicating whether or not to clear logs at startup.
    /// </summary>
    public bool ClearLogsAtStartup { get; init; } = false;
}
