// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Arc.Threading;

namespace Arc.Unit;

internal class FileLoggerWorker : TaskCore
{
    private const int MaxFlush = 10_000;
    private const int LimitLogThreshold = 10_000;

    public FileLoggerWorker(UnitCore core, UnitLogger unitLogger, FileLoggerOptions options)
        : base(core, Process, false)
    {
        this.logger = unitLogger.GetLogger<FileLoggerWorker>();
        this.formatter = new(options.Formatter);
        this.clearLogsAtStartup = options.ClearLogsAtStartup;

        this.maxCapacity = options.MaxLogCapacity * 1_000_000;
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

        while (worker.Sleep(1_000))
        {
            await worker.Flush(false).ConfigureAwait(false);
        }

        await worker.Flush(false);
    }

    public void Add(FileLoggerWork work)
    {
        this.queue.Enqueue(work);
    }

    public async Task<int> Flush(bool terminate)
    {
        await this.semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            StringBuilder sb = new();
            var count = 0;
            while (count < MaxFlush && this.queue.TryDequeue(out var work))
            {
                count++;
                this.formatter.Format(sb, work.Parameter);
                sb.Append(Environment.NewLine);
            }

            if (count != 0)
            {
                var path = this.GetCurrentPath();
                if (Path.GetDirectoryName(path) is { } directory)
                {
                    PathHelper.TryCreateDirectory(directory);
                }

                try
                {
                    await File.AppendAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
                }
                catch
                {
                }
            }

            if (terminate)
            {
                this.Terminate();
            }
            else
            {// Limit log capacity
                this.limitLogCount += count;
                var now = DateTime.UtcNow;
                if (now - this.limitLogTime > TimeSpan.FromMinutes(10) ||
                    this.limitLogCount >= LimitLogThreshold)
                {
                    this.limitLogTime = now;
                    this.limitLogCount = 0;

                    this.LimitLogs(false);
                }
            }

            return count;
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

        // this.logger?.TryGet()?.Log($"Limit logs {capacity}/{this.maxCapacity} {directory}");
        foreach (var x in pathToSize)
        {
            if (!removeAll && capacity < this.maxCapacity)
            {
                break;
            }

            try
            {
                File.Delete(x.Key);
                this.logger?.TryGet()?.Log($"Deleted: {x.Key}");
            }
            catch
            {
            }

            capacity -= x.Value;
        }
    }

    public int Count => this.queue.Count;

    private ILogger<FileLoggerWorker>? logger;
    private string basePath;
    private string baseFile;
    private string baseExtension;
    private SimpleLogFormatter formatter;
    private ConcurrentQueue<FileLoggerWork> queue = new();
    private SemaphoreSlim semaphore = new(1, 1);
    private DateTime limitLogTime;
    private int limitLogCount = 0;
    private long maxCapacity;
    private bool clearLogsAtStartup;
}

internal class FileLoggerWork
{
    public FileLoggerWork(LogEvent parameter)
    {
        this.Parameter = parameter;
    }

    public LogEvent Parameter { get; }
}
