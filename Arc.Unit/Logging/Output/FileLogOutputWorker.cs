// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
using Arc.Threading;
using Utf8StringInterpolation;

namespace Arc.Unit;

/// <summary>
/// Background worker which writes the buffered logs of <see cref="FileLogger{TOption}"/> to a file, and limits the log capacity.
/// </summary>
internal sealed class FileLoggerWorker : TaskCore
{
    private const int MaxFlush = 10_000;
    private const int LimitLogThreshold = 10_000;
    private const int IntervalInMilliseconds = 1_000;

    private readonly SimpleLogFormatter formatter;
    private readonly LogEventQueue queue = new();
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly string basePath;
    private readonly string baseFile;
    private readonly string baseExtension;
    private readonly long maxCapacity;
    private readonly Lock pathLock = new();
    private readonly string directoryPath;
    private ArrayBufferWriter<byte> buffer = new();
    private DateTime pathDate;
    private string? currentPath;
    private DateTime limitLogTime;
    private int limitLogCount;

    public int Count => this.queue.Count;

    public FileLoggerWorker(ExecutionRoot root, FileLoggerOptions options)
        : base(LogUnit.GetGroup(root), Process, ExecutionCoreOptions.DelayedStart)
    {
        this.formatter = new(options.FormatterOptions);

        this.maxCapacity = (long)options.MaxLogCapacity * 1_000_000;
        var fullPath = options.Path;
        var fileName = Path.GetFileName(fullPath);
        var idx = fileName.LastIndexOf('.'); // "TestLog.txt" -> 7
        if (idx >= 0)
        {
            idx += fullPath.Length - fileName.Length;
            this.basePath = fullPath.Substring(0, idx);
            this.baseExtension = fullPath.Substring(idx);
        }
        else
        {
            this.basePath = fullPath;
            this.baseExtension = string.Empty;
        }

        this.baseFile = Path.GetFileName(this.basePath);
        this.directoryPath = Path.GetDirectoryName(this.basePath) ?? string.Empty;
        if (options.ClearLogsAtStartup)
        {
            this.LimitLogs(true);
        }
    }

    public static async Task Process(object? obj)
    {
        var worker = (FileLoggerWorker)obj!;

        while (await worker.TryDelay(IntervalInMilliseconds))
        {
            await worker.Flush(false).ConfigureAwait(false);
        }

        await worker.Flush(true).ConfigureAwait(false); // Flush the remaining logs.
    }

    public void Add(LogEvent logEvent, int maxQueue = 0)
    {
        this.queue.Enqueue(logEvent, maxQueue);
    }

    public async Task<int> Flush(bool terminate)
    {
        await this.semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (terminate)
            {
                this.queue.Complete();
            }

            var total = 0;
            while (true)
            {
                var count = this.DequeueUtf8();
                total += count;
                if (this.buffer.WrittenCount > 0)
                {
                    var path = this.GetCurrentPath();
                    if (this.directoryPath.Length > 0)
                    {
                        PathHelper.TryCreateDirectory(this.directoryPath);
                    }

                    await PathHelper.TryAppendAllBytes(path, this.buffer.WrittenMemory).ConfigureAwait(false);
                }

                if (!terminate || count < MaxFlush)
                {// Flush all the queued logs on termination.
                    break;
                }
            }

            if (terminate)
            {
                this.RequestTermination();
            }
            else
            {// Limit log capacity
                this.limitLogCount += total;
                var now = DateTime.UtcNow;
                if (now - this.limitLogTime > TimeSpan.FromMinutes(10) ||
                    this.limitLogCount >= LimitLogThreshold)
                {
                    this.limitLogTime = now;
                    this.limitLogCount = 0;

                    this.LimitLogsCore(false);
                }
            }

            return total;
        }
        finally
        {
            if (this.buffer.Capacity > 1024 * 1024)
            {
                this.buffer = new();
            }
            else
            {
                this.buffer.ResetWrittenCount();
            }

            this.semaphore.Release();
        }
    }

    internal string GetCurrentPath()
    {// The invariant culture is required, so that the file name does not depend on the current culture/calendar.
        lock (this.pathLock)
        {
            var today = DateTime.UtcNow.Date;
            if (this.currentPath is null || this.pathDate != today)
            {
                Span<char> date = stackalloc char[8];
                today.TryFormat(date, out var written, "yyyyMMdd", CultureInfo.InvariantCulture);
                this.currentPath = string.Concat(this.basePath, date.Slice(0, written), this.baseExtension);
                this.pathDate = today;
            }

            return this.currentPath;
        }
    }

    internal void LimitLogs(bool removeAll)
    {
        this.semaphore.Wait();
        try
        {
            this.LimitLogsCore(removeAll);
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    private void LimitLogsCore(bool removeAll)
    {
        var currentPath = this.GetCurrentPath();
        var directory = Path.GetDirectoryName(currentPath);
        var file = Path.GetFileName(currentPath);
        if (directory == null || file == null)
        {
            return;
        }

        long capacity = 0;
        List<(string Path, long Size)> files = new();
        try
        {
            foreach (var x in Directory.EnumerateFiles(directory, this.baseFile + "*" + this.baseExtension, SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(x).AsSpan();
                if (name.Length == this.baseFile.Length + 8 + this.baseExtension.Length &&
                    name.StartsWith(this.baseFile, StringComparison.Ordinal) &&
                    name.EndsWith(this.baseExtension, StringComparison.Ordinal) &&
                    DateOnly.TryParseExact(name.Slice(this.baseFile.Length, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    try
                    {
                        var size = new FileInfo(x).Length;
                        files.Add((x, size));
                        capacity += size;
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
            return;
        }

        files.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));
        foreach (var x in files)
        {// Delete the old logs (the file name contains the date, so the dictionary is sorted in chronological order).
            if (!removeAll && capacity <= this.maxCapacity)
            {
                break;
            }

            if (PathHelper.TryDeleteFile(x.Path))
            {
                capacity -= x.Size;
            }
        }
    }

    /// <summary>
    /// Formats one batch into the reusable buffer while the flush semaphore is held.
    /// </summary>
    /// <returns>The number of dequeued logs.</returns>
    private int DequeueUtf8()
    {
        this.buffer.ResetWrittenCount();
        var writer = new Utf8StringWriter<ArrayBufferWriter<byte>>(this.buffer);
        var count = 0;
        while (count < MaxFlush && this.queue.TryDequeue(out var logEvent))
        {
            count++;
            this.formatter.FormatUtf8(ref writer, logEvent);
        }

        writer.Flush();
        return count;
    }
}
