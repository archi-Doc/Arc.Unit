// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Arc.Threading;

namespace Arc.Unit;

/// <summary>
/// Background worker which writes the buffered logs of <see cref="ConsoleLogger"/> to the console.
/// </summary>
internal sealed class ConsoleLoggerWorker : TaskCore
{
    private const int MaxFlush = 1_000;
    private const int BufferingTimeInMilliseconds = 40;

    private readonly ConsoleLogger consoleLogger;
    private readonly ConcurrentQueue<LogEvent> queue = new();

    public ConsoleLoggerWorker(ExecutionRoot root, ConsoleLogger consoleLogger)
        : base(LogUnit.GetGroup(root), Process)
    {
        this.consoleLogger = consoleLogger;
    }

    public static async Task Process(TaskCore obj)
    {
        var worker = (ConsoleLoggerWorker)obj!;
        while (await worker.Delay(BufferingTimeInMilliseconds))
        {
            await worker.Flush(false).ConfigureAwait(false);
        }

        await worker.Flush(true).ConfigureAwait(false); // Flush the remaining logs.
    }

    public void Add(LogEvent logEvent)
    {
        this.queue.Enqueue(logEvent);
    }

    public Task<int> Flush(bool terminate)
    {
        var count = 0;
        var maxFlush = terminate ? int.MaxValue : MaxFlush; // Flush all the queued logs on termination.
        var formatter = this.consoleLogger.Formatter;
        while (count < maxFlush && this.queue.TryDequeue(out var logEvent))
        {
            count++;

            // Console output might cause unexpected exceptions after the console window is closed (IConsoleService handles them).
            formatter.FormatAndWriteLine(logEvent.LogService.ConsoleService, logEvent);
        }

        if (terminate)
        {
            this.RequestTermination();
        }

        return Task.FromResult(count);
    }

    public int Count => this.queue.Count;
}
