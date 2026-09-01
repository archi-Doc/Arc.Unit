// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentQueue<LogEvent> queue = new();
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly string basePath;
    private readonly string baseFile;
    private readonly string baseExtension;
    private readonly long maxCapacity;
    private readonly bool clearLogsAtStartup;
    private DateTime limitLogTime;
    private int limitLogCount;

    public int Count => this.queue.Count;

    public FileLoggerWorker(ExecutionRoot root, FileLoggerOptions options)
        : base(LogUnit.GetGroup(root), Process, ExecutionCoreOptions.DelayedStart)
    {
        this.formatter = new(options.FormatterOptions);
        this.clearLogsAtStartup = options.ClearLogsAtStartup;

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
    }

    public static async Task Process(object? obj)
    {
        var worker = (FileLoggerWorker)obj!;

        if (worker.clearLogsAtStartup)
        {
            worker.LimitLogs(true);
        }

        while (await worker.Delay(IntervalInMilliseconds))
        {
            await worker.Flush(false).ConfigureAwait(false);
        }

        await worker.Flush(true).ConfigureAwait(false); // Flush the remaining logs.
    }

    public void Add(LogEvent logEvent)
    {
        this.queue.Enqueue(logEvent);
    }

    public async Task<int> Flush(bool terminate)
    {
        await this.semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var total = 0;
            while (true)
            {
                (var count, var bytes) = this.DequeueUtf8();
                total += count;
                if (bytes.Length > 0)
                {
                    var path = this.GetCurrentPath();
                    if (Path.GetDirectoryName(path) is { } directory)
                    {
                        PathHelper.TryCreateDirectory(directory);
                    }

                    await PathHelper.TryAppendAllBytes(path, bytes).ConfigureAwait(false);
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

                    this.LimitLogs(false);
                }
            }

            return total;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    internal string GetCurrentPath()
        => this.basePath + DateTime.UtcNow.ToString("yyyyMMdd") + this.baseExtension;

    internal void LimitLogs(bool removeAll)
    {
        var currentPath = this.GetCurrentPath();
        var directory = Path.GetDirectoryName(currentPath);
        var file = Path.GetFileName(currentPath);
        if (directory == null || file == null)
        {
            return;
        }

        long capacity = 0;
        SortedDictionary<string, long> pathToSize = new();
        try
        {
            foreach (var x in Directory.EnumerateFiles(directory, this.baseFile + "*" + this.baseExtension, SearchOption.TopDirectoryOnly))
            {
                if (x.Length == currentPath.Length)
                {
                    try
                    {
                        var size = new FileInfo(x).Length;
                        pathToSize.Add(x, size);
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

        foreach (var x in pathToSize)
        {// Delete the old logs (the file name contains the date, so the dictionary is sorted in chronological order).
            if (!removeAll && capacity < this.maxCapacity)
            {
                break;
            }

            PathHelper.TryDeleteFile(x.Key);
            capacity -= x.Value;
        }
    }

    /// <summary>
    /// Dequeues the log events and converts them into a UTF-8 byte array.
    /// </summary>
    /// <returns>The number of dequeued logs and the UTF-8 byte array.</returns>
    private (int Count, byte[] Bytes) DequeueUtf8()
    {
        using var buffer = Utf8String.CreateWriter(out var writer);
        var count = 0;
        while (count < MaxFlush && this.queue.TryDequeue(out var logEvent))
        {
            count++;
            this.formatter.FormatUtf8(ref writer, logEvent);
        }

        if (count == 0)
        {
            return (0, Array.Empty<byte>());
        }

        writer.Flush();
        return (count, buffer.ToArray());
    }
}
