// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

/// <summary>
/// Interface for a log output which writes logs to a file.
/// </summary>
public interface IFileLogOutput
{
    /// <summary>
    /// Gets the path of the log file which is currently used (the file name contains the date).
    /// </summary>
    /// <returns>The path of the log file.</returns>
    string GetCurrentPath();

    /// <summary>
    /// Deletes matching daily files, serialized with writes. Queued events are retained and may recreate a file.
    /// </summary>
    void DeleteAllLogs();

    /// <summary>
    /// Writes the buffered logs to the log file.
    /// </summary>
    /// <param name="terminate"><see langword="true" /> to write all the buffered logs and terminate the log worker.</param>
    /// <returns>The number of dequeued events, even if best-effort file writes fail.</returns>
    Task<int> FlushAsync(bool terminate);
}

/// <summary>
/// <see cref="ILogOutput"/> which writes logs to a file (one file per day).<br/>
/// Logs are buffered and written by a background worker, and the total capacity is limited by <see cref="FileLogOutputOptions.MaxLogCapacityInMegabytes"/>.
/// </summary>
/// <remarks>Each log output must own a distinct path. Writes are best-effort and failed batches are not retried.
/// Normal flushes process up to 10,000 events; terminating flushes close the queue and drain accepted events.</remarks>
/// <typeparam name="TOptions">The type of options which determines the file path and the behavior.</typeparam>
public class FileLogOutput<TOptions> : BufferedLogOutput, IFileLogOutput
    where TOptions : FileLogOutputOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogOutput{TOptions}"/> class.
    /// </summary>
    /// <param name="root"><see cref="ExecutionRoot"/> which owns the background worker.</param>
    /// <param name="logUnit"><see cref="LogUnit"/>.</param>
    /// <param name="options">The options which determines the file path and the behavior.</param>
    public FileLogOutput(ExecutionRoot root, LogUnit logUnit, TOptions options)
        : base(logUnit)
    {
        if (!Path.IsPathFullyQualified(options.FilePath))
        {// Relative to the current directory.
            options = options with { FilePath = Path.GetFullPath(options.FilePath), };
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
        this.worker.Add(logEvent, this.options.MaxQueueLength);
    }

    /// <inheritdoc/>
    public override Task<int> FlushAsync(bool terminate) => this.worker.FlushAsync(terminate);

    private readonly FileLogOutputWorker worker;
    private readonly TOptions options;
}
