// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

/// <summary>
/// Interface for a log output which writes logs to a file.
/// </summary>
public interface IFileLogger
{
    /// <summary>
    /// Gets the path of the log file which is currently used (the file name contains the date).
    /// </summary>
    /// <returns>The path of the log file.</returns>
    string GetCurrentPath();

    /// <summary>
    /// Deletes all the log files created by this logger.
    /// </summary>
    void DeleteAllLogs();

    /// <summary>
    /// Writes the buffered logs to the log file.
    /// </summary>
    /// <param name="terminate"><see langword="true" /> to write all the buffered logs and terminate the log worker.</param>
    /// <returns>The number of flushed logs.</returns>
    Task<int> Flush(bool terminate);
}

/// <summary>
/// <see cref="ILogOutput"/> which writes logs to a file (one file per day).<br/>
/// Logs are buffered and written by a background worker, and the total capacity is limited by <see cref="FileLoggerOptions.MaxLogCapacity"/>.
/// </summary>
/// <typeparam name="TOption">The type of options which determines the file path and the behavior.</typeparam>
public class FileLogger<TOption> : BufferedLogOutput, IFileLogger
    where TOption : FileLoggerOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger{TOption}"/> class.
    /// </summary>
    /// <param name="root"><see cref="ExecutionRoot"/> which owns the background worker.</param>
    /// <param name="logUnit"><see cref="LogUnit"/>.</param>
    /// <param name="options">The options which determines the file path and the behavior.</param>
    public FileLogger(ExecutionRoot root, LogUnit logUnit, TOption options)
        : base(logUnit)
    {
        if (string.IsNullOrEmpty(Path.GetDirectoryName(options.Path)))
        {// Relative to the current directory.
            options = options with { Path = Path.Combine(Directory.GetCurrentDirectory(), options.Path), };
        }

        this.worker = new(root, options);
        this.options = options;
        this.worker.SendSignal(ExecutionSignal.Start);
    }

    /// <inheritdoc/>
    public string GetCurrentPath()
        => this.worker.GetCurrentPath();

    /// <inheritdoc/>
    public void DeleteAllLogs()
        => this.worker.LimitLogs(true);

    /// <inheritdoc/>
    public override void Output(LogEvent logEvent)
    {
        if (this.options.MaxQueue <= 0 || this.worker.Count < this.options.MaxQueue)
        {
            this.worker.Add(logEvent);
        }
    }

    /// <inheritdoc/>
    public override Task<int> Flush(bool terminate) => this.worker.Flush(terminate);

    private readonly FileLoggerWorker worker;
    private readonly TOption options;
}
