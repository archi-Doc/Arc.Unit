// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Arc.Unit;

internal sealed class ConsoleKeyReader
{
    private readonly Task task;
    private readonly ConcurrentQueue<ConsoleKeyInfo> queue =
        new();

    public ConsoleKeyReader(CancellationToken cancellationToken = default)
    {
        this.task = new Task(
            () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var keyInfo = Console.ReadKey(intercept: true);
                        this.queue.Enqueue(keyInfo);
                    }
                    catch
                    {
                        Thread.Sleep(10);
                    }
                }
            },
            cancellationToken,
            TaskCreationOptions.LongRunning);

        this.task.Start();
    }

    public bool TryRead(out ConsoleKeyInfo keyInfo)
    {
        return this.queue.TryDequeue(out keyInfo);
    }

    public bool IsKeyAvailable => !this.queue.IsEmpty;
}
