// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Arc.Threading;

namespace Arc.Unit;

internal class ConsoleLoggerWorker : TaskCore
{
    private const int MaxFlush = 1_000;
    private const int BufferingTimeInMilliseconds = 40;

    public ConsoleLoggerWorker(UnitCore core, ConsoleLogger consoleLogger)
        : base(core, Process)
    {
        this.consoleLogger = consoleLogger;
    }

    public static async Task Process(object? obj)
    {
        var worker = (ConsoleLoggerWorker)obj!;
        while (await worker.Delay(BufferingTimeInMilliseconds))
        {
            await worker.Flush(false).ConfigureAwait(false);
        }
    }

    public void Add(ConsoleLoggerWork work)
    {
        this.queue.Enqueue(work);
    }

    public async Task<int> Flush(bool terminate)
    {
        var count = 0;
        while (count < MaxFlush && this.queue.TryDequeue(out var work))
        {
            count++;
            try
            {// Console.WriteLine() might cause unexpected exceptions after console window is closed.
                Console.WriteLine(this.consoleLogger.Formatter.Format(work.Parameter));
            }
            catch
            {
            }
        }

        if (terminate)
        {
            this.Terminate();
        }

        return count;
    }

    public int Count => this.queue.Count;

    private ConsoleLogger consoleLogger;
    private ConcurrentQueue<ConsoleLoggerWork> queue = new();
}

internal class ConsoleLoggerWork : ThreadWork
{
    public ConsoleLoggerWork(LogEvent parameter)
    {
        this.Parameter = parameter;
    }

    public LogEvent Parameter { get; }
}
