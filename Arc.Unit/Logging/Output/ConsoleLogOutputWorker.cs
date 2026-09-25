// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

/// <summary>
/// Background worker which writes the buffered logs of <see cref="ConsoleLogOutput"/> to the console.
/// </summary>
internal sealed class ConsoleLogOutputWorker : TaskCore
{
    private const int MaxFlush = 1_000;
    private const int BufferingTimeInMilliseconds = 40;

    private readonly ConsoleLogOutput consoleLogOutput;
    private readonly LogEventQueue queue = new();
    private readonly Lock flushLock = new();

    public ConsoleLogOutputWorker(ExecutionRoot root, ConsoleLogOutput consoleLogOutput)
        : base(LogUnit.GetGroup(root), Process, ExecutionCoreOptions.DelayedStart)
    {
        this.consoleLogOutput = consoleLogOutput;
        this.SendSignal(ExecutionSignal.Start);
    }

    public static async Task Process(TaskCore obj)
    {
        var worker = (ConsoleLogOutputWorker)obj!;
        while (await worker.TryDelay(BufferingTimeInMilliseconds))
        {
            await worker.FlushAsync(false).ConfigureAwait(false);
        }

        await worker.FlushAsync(true).ConfigureAwait(false); // Flush the remaining logs.
    }

    public void Add(LogEvent logEvent, int maxQueueLength)
    {
        this.queue.Enqueue(logEvent, maxQueueLength);
    }

    public Task<int> FlushAsync(bool terminate)
    {
        lock (this.flushLock)
        {
            return this.FlushCore(terminate);
        }
    }

    private Task<int> FlushCore(bool terminate)
    {
        if (terminate)
        {
            this.queue.Complete();
        }

        var count = 0;
        var maxFlush = terminate ? int.MaxValue : MaxFlush; // Flush all the queued logs on termination.
        var formatter = this.consoleLogOutput.Formatter;
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
}
